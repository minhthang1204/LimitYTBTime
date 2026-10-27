package vn.limityt.tv.sync

import android.content.Context
import android.os.Handler
import android.os.Looper
import android.util.Log
import kotlinx.serialization.json.Json
import vn.limityt.tv.LimitApp
import java.io.BufferedReader
import java.io.InputStreamReader
import java.net.ServerSocket
import java.net.Socket
import java.util.concurrent.atomic.AtomicBoolean

class SyncServer(private val context: Context, private val port: Int = 47601) : Thread("SyncServer") {
    private val running = AtomicBoolean(true)
    private var serverSocket: ServerSocket? = null
    private val mainHandler = Handler(Looper.getMainLooper())
    private val json = Json { ignoreUnknownKeys = true }

    override fun run() {
        try {
            serverSocket = ServerSocket(port)
            Log.i("LimitYT_Sync", "SyncServer started on port $port")
            while (running.get()) {
                val client = serverSocket?.accept() ?: break
                handleClient(client)
            }
        } catch (e: Exception) {
            if (running.get()) Log.e("LimitYT_Sync", "SyncServer error", e)
        }
    }

    private fun handleClient(socket: Socket) {
        Thread {
            try {
                socket.soTimeout = 8000
                val reader = BufferedReader(InputStreamReader(socket.getInputStream(), Charsets.UTF_8))
                val firstLine = reader.readLine() ?: return@Thread
                if (!firstLine.startsWith("POST")) {
                    val res = "HTTP/1.1 405 Method Not Allowed\r\nConnection: close\r\n\r\n"
                    socket.getOutputStream().write(res.toByteArray())
                    socket.close()
                    return@Thread
                }
                var contentLength = 0
                while (true) {
                    val line = reader.readLine() ?: break
                    if (line.isEmpty()) break
                    if (line.startsWith("Content-Length:", ignoreCase = true)) {
                        contentLength = line.substring(15).trim().toIntOrNull() ?: 0
                    }
                }
                val body = CharArray(contentLength)
                var readTotal = 0
                while (readTotal < contentLength) {
                    val r = reader.read(body, readTotal, contentLength - readTotal)
                    if (r == -1) break
                    readTotal += r
                }
                val bodyStr = String(body, 0, readTotal)
                val incomingPacket = json.decodeFromString<SyncPacket>(bodyStr)

                var replyJson = ""
                val lock = Object()
                synchronized(lock) {
                    mainHandler.post {
                        try {
                            val replyPacket = LimitApp.guardian(context).applySync(incomingPacket)
                            replyJson = json.encodeToString(SyncPacket.serializer(), replyPacket)
                        } finally {
                            synchronized(lock) { lock.notifyAll() }
                        }
                    }
                    lock.wait(4000)
                }

                val replyBytes = replyJson.toByteArray(Charsets.UTF_8)
                val response = "HTTP/1.1 200 OK\r\n" +
                        "Content-Type: application/json; charset=utf-8\r\n" +
                        "Content-Length: ${replyBytes.size}\r\n" +
                        "Connection: close\r\n\r\n"
                socket.getOutputStream().write(response.toByteArray())
                socket.getOutputStream().write(replyBytes)
                socket.getOutputStream().flush()
            } catch (e: Exception) {
                Log.w("LimitYT_Sync", "Sync request error: ${e.message}")
            } finally {
                runCatching { socket.close() }
            }
        }.start()
    }

    fun stopServer() {
        running.set(false)
        runCatching { serverSocket?.close() }
    }
}

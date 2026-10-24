import java.util.Properties

plugins {
    id("com.android.application")
    id("org.jetbrains.kotlin.android")
    id("org.jetbrains.kotlin.plugin.serialization")
}

// Số phiên bản dùng chung với PC: file VERSION ở thư mục gốc ("0.9.0" → versionCode 900).
val appVersion = rootProject.file("../VERSION").readText().trim()
val appVersionCode = appVersion.split('.').map { it.toInt() }.let { (major, minor, patch) -> major * 10_000 + minor * 100 + patch }

// Keystore release nằm ngoài repo (tools/dev-env.ps1 đặt LIMITYT_SIGNING; tv/build-tv.ps1 tạo nếu chưa có).
val signingProps = System.getenv("LIMITYT_SIGNING")?.let(::file)?.takeIf { it.isFile }?.let { f ->
    Properties().apply { f.inputStream().use(::load) }.also { it["storeFile"] = f.resolveSibling(it.getProperty("storeFile")).path }
}

android {
    namespace = "vn.limityt.tv"
    compileSdk = 35
    buildToolsVersion = "35.0.0"

    defaultConfig {
        applicationId = "vn.limityt.tv"
        minSdk = 28
        targetSdk = 35
        versionCode = appVersionCode
        versionName = appVersion
    }

    signingConfigs {
        if (signingProps != null) create("release") {
            storeFile = file(signingProps.getProperty("storeFile"))
            storePassword = signingProps.getProperty("storePassword")
            keyAlias = signingProps.getProperty("keyAlias")
            keyPassword = signingProps.getProperty("keyPassword")
        }
    }

    buildTypes {
        release {
            isMinifyEnabled = false
            // Phải ký cùng một khoá thì mới cài đè bản mới được mà không mất dữ liệu trên TV.
            signingConfig = if (signingProps != null) signingConfigs.getByName("release") else {
                logger.warn("LIMITYT_SIGNING chưa đặt: APK release được ký bằng debug key")
                signingConfigs.getByName("debug")
            }
        }
    }

    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_17
        targetCompatibility = JavaVersion.VERSION_17
    }

    // Bộ ca kiểm thử dùng chung với PC: spec/rules-test-vectors.json
    sourceSets["test"].resources.srcDir("../../spec")

    lint {
        checkReleaseBuilds = false
    }
}

kotlin {
    compilerOptions {
        jvmTarget.set(org.jetbrains.kotlin.gradle.dsl.JvmTarget.JVM_17)
    }
}

dependencies {
    implementation("org.jetbrains.kotlinx:kotlinx-serialization-json:1.7.3")
    testImplementation("junit:junit:4.13.2")
}

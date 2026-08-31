# AdbWirelessToolkitGUI

![License](https://img.shields.io/badge/License-Dual%20(MIT%20%7C%20GPLv3)-blue.svg)
![.NET](https://img.shields.io/badge/.NET-8.0-512BD4.svg)
![Platform](https://img.shields.io/badge/Platform-Windows-lightgrey.svg)
![WPF](https://img.shields.io/badge/UI-WPF-0078D4.svg)

> **Herramienta gráfica moderna para gestión inalámbrica de dispositivos Android vía ADB**  
> Interfaz WPF nativa estilo Windows 11 con escaneo de red, emparejamiento, sideload y terminal manual integrado.

---

## 📋 Descripción

**AdbWirelessToolkitGUI** es una aplicación de escritorio Windows (WPF, .NET 8) diseñada para simplificar la administración de dispositivos Android mediante ADB (Android Debug Bridge) sin necesidad de cables USB. Proporciona una interfaz gráfica intuitiva estilo Windows 11 que permite:

- 🔍 **Escanear la red local** para descubrir dispositivos Android con depuración TCP/IP activa
- 📱 **Emparejar dispositivos** mediante código de emparejamiento (Android 11+)
- 📋 **Listar dispositivos conectados** y su estado
- 🔄 **Reiniciar el servidor ADB** con un clic
- 📲 **Instalar APKs** de forma inalámbrica
- 📦 **Sideload APK** desde modo Recovery (USB)
- 📄 **Transferir archivos** (push/pull) con barra de progreso
- 🔄 **Reiniciar dispositivo** al sistema normal
- 💻 **Terminal manual** para comandos ADB arbitrarios
- 🖥 **Abrir CMD externo** en la carpeta PlatformTools como respaldo total

---

## ✨ Características Principales

| Función | Descripción |
|---------|-------------|
| **Escaneo de Red Local** | Detecta automáticamente la subred y escanea IPs `.1` a `.99` con `Ping` concurrente (20 hilos, timeout 500ms) |
| **Emparejamiento Wi-Fi** | Soporte nativo para `adb pair <host:port> <código>` (Android 11+) |
| **Ejecución Asíncrona** | Todos los comandos corren en background sin bloquear la UI (`async/await` + `CancellationToken`) |
| **Progreso en Tiempo Real** | Regex robusto captura porcentajes de `adb push`, `adb sideload`, `adb install` |
| **Terminal Manual** | Ejecuta cualquier comando ADB raw con autocompletado de prefijo `adb` |
| **CMD Externo** | Abre consola nativa en `PlatformTools/` para control total de emergencia |
| **Dual License** | MIT  y GNU GPL v3 |
| **Auto-Elevación UAC** | Instalador requiere admin; app se ejecuta como usuario normal |
| **Telemetría de Errores** | Logging automático de excepciones no controladas en `AdbWirelessToolkitGUI_Log.txt` |

---

## 🖥 Capturas de Pantalla

> *Agregar capturas aquí cuando el proyecto esté en GitHub*

---

## ⚙️ Requisitos Previos

| Requisito | Versión Mínima | Notas |
|-----------|----------------|-------|
| **.NET SDK** | 8.0 | [Descargar](https://dotnet.microsoft.com/download/dotnet/8.0) |
| **Windows** | 10/11 (x64) | Requiere WPF |
| **Android SDK Platform-Tools** | Incluido | Carpeta `PlatformTools/` con `adb.exe`, `AdbWinApi.dll`, `AdbWinUsbApi.dll`, `fastboot.exe` |
| **Dispositivo Android** | Android 5.0+ | Depuración USB/TCP/IP habilitada |
| **Visual C++ Redistributable** | 2015-2022 | Opcional, instalable desde el instalador |
| **.NET 8.0.30 Desktop Runtime** | 8.0.30 | Opcional, instalable desde el instalador |

---

## 🛠 Cómo Compilar

### Opción A: Usando Makefile (Recomendado)

```bash
# Compilar en modo Debug
make build

# Publicar Self-Contained Single-File para x64
make publish-x64

# Publicar Self-Contained Single-File para x86
make publish-x86

# Limpiar artefactos
make clean

# Generar instalador MSI (requiere WiX Toolset)
make installer
```

### Opción B: Comandos `dotnet` Directos

```bash
# Restaurar dependencias
dotnet restore

# Compilar Debug
dotnet build --configuration Debug

# Compilar Release
dotnet build --configuration Release

# Publicar x64 (Single-file, Self-Contained, Trimmed)
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:PublishTrimmed=false -o ./publish/win-x64

# Publicar x86
dotnet publish -c Release -r win-x86 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:PublishTrimmed=false -o ./publish/win-x86

# Limpiar
dotnet clean

# Generar instalador MSI (requiere WiX Toolset)
dotnet build AdbWirelessToolkitGUI_Installer_Wix/AdbWirelessToolkitGUI_Installer.wixproj -c Release
```

### Estructura de Salida Esperada

```
publish/win-x64/
├── AdbWirelessToolkitGUI.exe          # Ejecutable principal (~200 MB single-file)
├── PlatformTools/                     # Copiado automáticamente
│   ├── adb.exe
│   ├── AdbWinApi.dll
│   ├── AdbWinUsbApi.dll
│   ├── fastboot.exe
│   └── ... (resto de binarios)
├── Assets/                            # Iconos y licencias
│   ├── Android-Logo-2008.ico
│   ├── Android-Logo-2008.png
│   ├── Combined-License_2.txt
│   ├── LICENSE-GPL3.rtf
│   └── LICENSE-MIT.rtf
└── RUNTIME/                           # Dependencias opcionales
    ├── VC_redist.x86.exe
    ├── VC_redist.x64.exe
    ├── windowsdesktop-runtime-8.0.30-win-x86.txt
    └── windowsdesktop-runtime-8.0.30-win-x64.txt
```

---

## 📦 Crear Instalador (WiX Toolset)

1. Instala [WiX Toolset v7+](https://wixtoolset.org/releases/)
2. Compila en Release para x64: `dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:PublishTrimmed=false -o ./publish/win-x64`
3. Compila el proyecto WiX: `dotnet build AdbWirelessToolkitGUI_Installer_Wix/AdbWirelessToolkitGUI_Installer.wixproj -c Release -p:SourceDir=../AdbWirelessToolkitGUI/publish/win-x64`
4. El instalador se genera en `AdbWirelessToolkitGUI_Installer_Wix/bin/Release/AdbWirelessToolkitGUI_Setup.msi`

### El instalador MSI incluye:
- ✅ **EULA con licencia dual (MIT/GPLv3)** usando `Combined-License_2.txt`
- ✅ Instala en `Program Files\AdbWirelessToolkitGUI`
- ✅ Incluye `PlatformTools/` completo
- ✅ **Pantalla de dependencias con 4 checkboxes**:
  - [ ] Visual C++ Redistributable (x86)
  - [ ] Visual C++ Redistributable (x64)
  - [ ] .NET 8.0.30 Desktop Runtime (x86)
  - [ ] .NET 8.0.30 Desktop Runtime (x64)
- ✅ Descarga automática de .NET 8.0.30 desde servidores oficiales de Microsoft
- ✅ Instalación silenciosa de VC++ Redistributables (x86/x64)
- ✅ Crea accesos directos en Menú Inicio y Escritorio (opcional)
- ✅ Registra desinstalador en Windows
- ✅ **EULA unificada** usando `Combined-License_2.txt` (MIT + GPLv3)

---

## 🚀 Uso Rápido

### Conexión Inalámbrica (Wi-Fi)

1. **En el dispositivo Android:**
   - Opciones de desarrollador → Depuración USB ✅
   - Depuración inalámbrica ✅ (Android 11+) → "Emparejar dispositivo con código de emparejamiento"
   - Anota el **IP:Puerto** y **Código de 6 dígitos**

2. **En la herramienta:**
   - Clic en **"🌐 Escanear Red Local (IPs)"** para descubrir IPs activas
   - Ingresa `HOST:PORT` y `CÓDIGO` en los campos correspondientes
   - Clic en **"📱 Emparejar Dispositivo"**
   - Verifica con **"📋 Dispositivos Conectados"**

### Conexión por Cable (USB)

1. Conecta dispositivo por USB con Depuración USB activada
2. Clic en **"📋 Dispositivos Conectados"** → debe aparecer `device`
3. Usa **"📲 Instalar APK"**, **"📦 Sideload APK (USB)"** o **"📄 Transferir Archivo"**

### Terminal Manual

- Escribe cualquier comando (ej: `shell pm list packages`, `logcat -d`)
- El prefijo `adb` se añade automáticamente
- Presiona **Enter** o clic en **"Ejecutar Comando"**

---

## 🚀 Características Avanzadas (v2.0+)

### 🔐 Sistema de Perfiles de Emparejamiento (Slots)
- **Hasta 10 perfiles guardados** en JSON (`%AppData%\AdbWirelessToolkitGUI\profiles.json`)
- Cada perfil almacena: `HOST:PORT`, `Código de 6 dígitos`, nombre personalizado opcional, timestamps
- **Menú desplegable** junto al botón "Emparejar" para cargar perfiles con un clic
- **Detección automática de éxito**: tras `adb pair`, si la salida contiene "Successfully"/"Success", pregunta si guardar
- **Gestión de límites**: al alcanzar 10 perfiles, permite seleccionar cuál sobrescribir
- Persistencia automática entre sesiones

### 🔍 Escaneo Dual de Red
| Modo | Comando | Descripción |
|------|---------|-------------|
| **Secuencial (Ping)** | `ScanLocalNetworkAsync` | Ping concurrente `.1-.99` (20 hilos, 500ms timeout) |
| **mDNS Nativo** | `adb mdns services` | Descubre servicios `_adb-tls-pairing._tcp` y `_adb-tls-connect._tcp` (requiere ADB 34+) |

- Parsing automático de IPs y puertos desde salida mDNS
- Identificación de tipo de servicio (Pairing vs Connect)
- Fallback informativo si ADB no soporta mDNS

### 📊 Precisión en Transferencias e Instalaciones
- **Regex mejorado**: captura velocidad (`XX.X MB/s`, `KB/s`, `GB/s`) + porcentaje en una pasada
- **Instalación APK (`adb install`)**: barra **indeterminada** durante "Performing Streamed Install" → 100% al recibir "Success"
- **Sideload (`adb sideload`)**: progreso real con porcentaje + velocidad
- **Push/Pull**: progreso estándar con velocidad en tiempo real
- Thread-safe: todo via `Dispatcher.BeginInvoke`

### ⚙️ Arquitectura Actualizada
- **`AdbEngine.TransferProgress` record**: `Percent`, `Speed`, `TransferType (Standard|Install|Sideload)`
- **`ProfileManager`**: Singleton con `ObservableCollection<DeviceProfile>`, JSON `System.Text.Json`
- **`SaveProfileWindow`**: Diálogo modal para nombrar/sobrescribir perfiles
- **ContextMenu dinámico**: Carga perfiles al vuelo desde `ProfileManager.Profiles`
- **Telemetría de Errores**: Logging global de excepciones en `AdbWirelessToolkitGUI_Log.txt`

---

## 📁 Estructura del Proyecto

```
AdbWirelessToolkitGUI/
├── AdbWirelessToolkitGUI.csproj              # Proyecto .NET 8 WPF
├── AdbWirelessToolkitGUI.sln                 # Solución
├── MainWindow.xaml                           # UI principal (Windows 11 style)
├── MainWindow.xaml.cs                        # Code-behind + lógica de comandos
├── App.xaml.cs                               # Punto de entrada + logging global + excepciones
├── App.xaml                                  # Recursos globales + idiomas
├── AdbEngine.cs                              # Motor ADB asíncrono + escáner red + mDNS
├── ProfileManager.cs                         # Gestión de perfiles (JSON en AppData)
├── AboutWindow.xaml / .cs                    # Ventana "Acerca de"
├── app.manifest                              # UAC asInvoker (app sin admin)
├── PlatformTools/                            # Binarios ADB (incluidos en build)
│   ├── adb.exe
│   ├── AdbWinApi.dll
│   ├── AdbWinUsbApi.dll
│   ├── fastboot.exe
│   └── ...
├── Assets/                                   # Iconos y licencias
│   ├── Android-Logo-2008.ico
│   ├── Android-Logo-2008.png
│   ├── LICENSE-GPL3.rtf
│   ├── LICENSE-MIT.rtf
│   └── Combined-License_2.txt
├── assets/RUNTIME/                           # Dependencias empaquetadas
│   ├── VC_redist.x86.exe
│   ├── VC_redist.x64.exe
│   ├── windowsdesktop-runtime-8.0.30-win-x86.txt
│   └── windowsdesktop-runtime-8.0.30-win-x64.txt
├── LICENSE-MIT.txt                           # Licencia MIT
├── LICENSE-GPL3.txt                          # Licencia GNU GPL v3
├── Combined-License_2.txt                    # Licencia dual unificada (EULA)
├── README.md                                 # Este archivo
├── Makefile                                  # Comandos de build estandarizados
└── AdbWirelessToolkitGUI_Installer_Wix/      # Instalador MSI (WiX Toolset)
    ├── Product.wxs                           # Definición principal del producto
    ├── Features.wxs                          # Definición de características
    ├── Files.wxs                             # Archivos a instalar
    ├── Dependencies.wxs                      # Dependencias (VC++, .NET)
    ├── UI.wxs                                # UI personalizada con 4 checkboxes
    └── AdbWirelessToolkitGUI_Installer.wixproj
```

---

## 🔧 Arquitectura Técnica

### Motor ADB (`AdbEngine.cs`)
- **`ExecuteCommandAsync`**: Comandos generales con captura stdout/stderr async
- **`ExecuteTransferAsync`**: Transferencias con parsing de progreso (`\d{1,3}\s*%`)
- **`ScanLocalNetworkAsync`**: Escaneo concurrente con `System.Net.NetworkInformation.Ping`
- **`ScanMdnsAsync`**: Escaneo mDNS nativo usando `adb mdns services`
- **Thread Safety**: Callbacks usan `Dispatcher.BeginInvoke` para actualizar UI
- **Cancelación**: `CancellationToken` global vinculado a `Window.Closed`
- **Timeouts**: Configurables por operación (defecto 5 min, install 10 min, push 30 min)

### UI (`MainWindow.xaml` + `.cs`)
- **Grid 2 columnas**: Panel acciones (izq) + Consola/Terminal (der)
- **ProgressBar + Labels**: Progreso % y velocidad en footer
- **Estilos Windows 11**: Colores `#1E1E1E`/`#252526`/`#3A3A3C`, acento `#0078D4`
- **Botones con Template**: Hover/Pressed/Disabled states customizados

### Sistema de Logging Global (`App.xaml.cs`)
- **Archivo de log**: `AdbWirelessToolkitGUI_Log.txt` (directorio del exe o `%LocalAppData%`)
- **Eventos capturados**:
  - `DispatcherUnhandledException` (UI Thread)
  - `AppDomain.CurrentDomain.UnhandledException` (Non-UI Threads)
  - `TaskScheduler.UnobservedTaskException` (Background Tasks)
  - `Application.Current.DispatcherUnhandledException` (WPF UI Exceptions)
- **Formato de log**: Timestamp, Tipo de excepción, Mensaje, StackTrace completo
- **Guardado sincronizado** antes de cerrar la aplicación

---

## 📄 Licencia Dual

Este proyecto se distribuye bajo **licencia dual**. Puedes elegir **UNA** de las siguientes:

### 🅰️ Licencia MIT (Permisiva)
- Uso comercial ✅
- Modificación ✅
- Distribución ✅
- Uso privado ✅
- Sublicenciamiento ✅
- **Sin obligación de publicar fuente**
- Ver: [`LICENSE-MIT.txt`](LICENSE-MIT.txt)

### 🅱️ Licencia GNU GPL v3 (Copyleft)
- Uso comercial ✅
- Modificación ✅
- Distribución ✅
- **Obligación de publicar fuente** de obras derivadas
- **Misma licencia** para derivados
- Ver: [`LICENSE-GPL3.txt`](LICENSE-GPL3.txt)

> **Archivo unificado para instaladores**: [`Combined-License_2.txt`](Combined-License_2.txt)

---

## 🤝 Contribuir

1. Fork del repositorio
2. Crea rama: `git checkout -b feature/nueva-funcionalidad`
3. Commit: `git commit -m "feat: descripción clara"`
4. Push: `git push origin feature/nueva-funcionalidad`
5. Abre **Pull Request**

### Estándares de Código
- C# 12 / .NET 8
- `Nullable` enabled, `ImplicitUsings` enabled
- `async/await` obligatorio para I/O
- `Dispatcher.BeginInvoke` para cruce de hilos UI
- Commits tipo [Conventional Commits](https://www.conventionalcommits.org/)

---

## 🐛 Reportar Issues

¿Encontraste un bug? ¿Tienes una sugerencia?
→ [Abrir Issue en GitHub](../../issues/new/choose)

Plantillas disponibles:
- 🐛 Bug Report
- 💡 Feature Request
- ❓ Pregunta/Soporte

---

## 🙏 Agradecimientos

- **Android Open Source Project** - ADB y Platform Tools
- **.NET Foundation** - Runtime y WPF
- **WiX Toolset** - Rob Mensching - Instalador MSI profesional
- **Comunidad Open Source** - Inspiración y feedback

---

## 📞 Contacto

- **Proyecto**: [GitHub Repository](../../)
- **Issues**: [GitHub Issues](../../issues)
- **Discusiones**: [GitHub Discussions](../../discussions)

---

<div align="center">

**¿Te gusta el proyecto? ¡Dale una ⭐ en GitHub!**

Hecho con ❤️ para la comunidad de desarrolladores Android

</div>

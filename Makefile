# =============================================================================
# Makefile - AdbWirelessToolkitGUI
# Sistema de compilación estandarizado usando .NET CLI
# =============================================================================

# Variables
PROJECT_FILE := AdbWirelessToolkitGUI.csproj
SOLUTION_FILE := AdbWirelessToolkitGUI.sln
CONFIGURATION := Release
OUTPUT_DIR := ./publish

# Detectar shell para comandos cross-platform
ifeq ($(OS),Windows_NT)
    DOTNET := dotnet.exe
    RM := rmdir /s /q
    MKDIR := mkdir
else
    DOTNET := dotnet
    RM := rm -rf
    MKDIR := mkdir -p
endif

# Targets por defecto
.DEFAULT_GOAL := help

# =============================================================================
# TARGETS PRINCIPALES
# =============================================================================

## Compilar en modo Debug
build:
	@echo "🔨 Compilando proyecto (Debug)..."
	$(DOTNET) build $(PROJECT_FILE) --configuration Debug --verbosity minimal
	@echo "✅ Build completado"

## Compilar en modo Release
build-release:
	@echo "🔨 Compilando proyecto (Release)..."
	$(DOTNET) build $(PROJECT_FILE) --configuration Release --verbosity minimal
	@echo "✅ Release build completado"

## Publicar Self-Contained Single-File para win-x64 (Producción)
publish-x64: clean
	@echo "📦 Publicando para win-x64 (Self-Contained, Single-File)..."
	$(DOTNET) publish $(PROJECT_FILE) \
		--configuration $(CONFIGURATION) \
		--runtime win-x64 \
		--self-contained true \
		-p:PublishSingleFile=true \
		-p:IncludeNativeLibrariesForSelfExtract=true \
		-p:PublishTrimmed=false \
		-p:DebugType=none \
		-p:DebugSymbols=false \
		-o $(OUTPUT_DIR)/win-x64
	@echo "✅ Publicación win-x64 completada en $(OUTPUT_DIR)/win-x64"

## Publicar Self-Contained Single-File para win-x86
publish-x86: clean
	@echo "📦 Publicando para win-x86 (Self-Contained, Single-File)..."
	$(DOTNET) publish $(PROJECT_FILE) \
		--configuration $(CONFIGURATION) \
		--runtime win-x86 \
		--self-contained true \
		-p:PublishSingleFile=true \
		-p:IncludeNativeLibrariesForSelfExtract=true \
		-p:PublishTrimmed=false \
		-p:DebugType=none \
		-p:DebugSymbols=false \
		-o $(OUTPUT_DIR)/win-x86
	@echo "✅ Publicación win-x86 completada en $(OUTPUT_DIR)/win-x86"

## Publicar ambas arquitecturas
publish-all: publish-x64 publish-x86
	@echo "✅ Todas las publicaciones completadas"

## Limpiar artefactos de compilación
clean:
	@echo "🧹 Limpiando artefactos..."
	$(DOTNET) clean $(PROJECT_FILE) --verbosity minimal
	@if exist $(OUTPUT_DIR) $(RM) $(OUTPUT_DIR) 2>nul || true
	@if exist bin $(RM) bin 2>nul || true
	@if exist obj $(RM) obj 2>nul || true
	@echo "✅ Limpieza completada"

## Restaurar dependencias NuGet
restore:
	@echo "📥 Restaurando dependencias..."
	$(DOTNET) restore $(SOLUTION_FILE) --verbosity minimal
	@echo "✅ Restauración completada"

## Ejecutar en modo Debug (con hot reload si está disponible)
run:
	@echo "▶️  Ejecutando en Debug..."
	$(DOTNET) run --project $(PROJECT_FILE) --configuration Debug

## Ejecutar tests (si existen)
test:
	@echo "🧪 Ejecutando tests..."
	$(DOTNET) test $(SOLUTION_FILE) --configuration $(CONFIGURATION) --verbosity normal

## Verificar formato de código (requiere dotnet-format)
format:
	@echo "🎨 Formateando código..."
	$(DOTNET) format $(SOLUTION_FILE) --verbosity minimal

## Verificar vulnerabilidades en paquetes NuGet
audit:
	@echo "🔍 Auditing paquetes NuGet..."
	$(DOTNET) list $(PROJECT_FILE) package --vulnerable --include-transitive

## Mostrar información del proyecto
info:
	@echo "ℹ️  Información del proyecto:"
	@echo "   Proyecto:     $(PROJECT_FILE)"
	@echo "   Solución:     $(SOLUTION_FILE)"
	@echo "   Config:       $(CONFIGURATION)"
	@echo "   Output:       $(OUTPUT_DIR)"
	@echo "   .NET SDK:     $$($(DOTNET) --version)"
	@echo "   Runtime:      $$($(DOTNET) --list-runtimes | head -1)"

# =============================================================================
# TARGETS DE INSTALADOR (requiere Inno Setup en PATH)
# =============================================================================

## Compilar instalador Inno Setup (Windows only)
ifeq ($(OS),Windows_NT)
ISCC := iscc.exe
else
ISCC := iscc
endif

installer: publish-x64
	@echo "📦 Compilando instalador Inno Setup..."
	$(ISCC) installer.iss
	@echo "✅ Instalador generado en Output/"

## Limpiar instalador
clean-installer:
	@if exist Output $(RM) Output 2>nul || true
	@echo "✅ Instalador limpiado"

# =============================================================================
# HELP
# =============================================================================

## Mostrar ayuda
help:
	@echo ""
	@echo "=================================================================="
	@echo "  AdbWirelessToolkitGUI - Makefile Targets"
	@echo "=================================================================="
	@echo ""
	@echo "  COMPILACIÓN:"
	@echo "    make build           - Compilar Debug"
	@echo "    make build-release   - Compilar Release"
	@echo ""
	@echo "  PUBLICACIÓN (Self-Contained Single-File):"
	@echo "    make publish-x64     - Publicar para Windows x64"
	@echo "    make publish-x86     - Publicar para Windows x86"
	@echo "    make publish-all     - Publicar ambas arquitecturas"
	@echo ""
	@echo "  MANTENIMIENTO:"
	@echo "    make clean           - Limpiar bin/obj/publish"
	@echo "    make restore         - Restaurar paquetes NuGet"
	@echo "    make run             - Ejecutar en Debug"
	@echo "    make test            - Ejecutar tests"
	@echo "    make format          - Formatear código (dotnet-format)"
	@echo "    make audit           - Auditar vulnerabilidades NuGet"
	@echo "    make info            - Mostrar info del proyecto"
	@echo ""
	@echo "  INSTALADOR (Windows + Inno Setup):"
	@echo "    make installer       - Compilar instalador .exe"
	@echo "    make clean-installer - Limpiar carpeta Output/"
	@echo ""
	@echo "=================================================================="
	@echo ""

.PHONY: build build-release publish-x64 publish-x86 publish-all clean restore run test format audit info installer clean-installer help
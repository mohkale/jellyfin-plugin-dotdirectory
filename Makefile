$(V).SILENT:

.PHONY: build
build:
	dotnet build

.PHONY: build-release
build-release:
	dotnet build /property:GenerateFullPaths=true /consoleloggerparameters:NoSummary -c Release

.PHONY: release
RELEASE_ARCHIVE := Jellyfin.Plugin.DotDirectory.zip
release: build-release
	echo
	echo "Creating release archive:"
	base=$$(pwd); cd Jellyfin.Plugin.DotDirectory/bin/Release/net6.0 && zip -9r "$$base/$(RELEASE_ARCHIVE)" .
	echo
	echo "Release checksum:"
	echo "  md5sum: $$(md5sum $(RELEASE_ARCHIVE) | cut -d ' ' -f1)"
	echo "  sha256: $$(sha256sum $(RELEASE_ARCHIVE) | cut -d ' ' -f1)"

.PHONY: lint
lint: format

.PHONY: format
format:
	dotnet format --verify-no-changes

from conan import ConanFile
from conan.tools.files import load, collect_libs
from conan.tools.cmake import CMakeToolchain, CMake, cmake_layout, CMakeDeps
import re

def ReadNameVersionFromCmake(recipe):
    # Reads the PROJECT(NAME ... VERSION X.Y.Z ...) from the CMake
    pattern = re.compile(r"PROJECT\(\s*(?P<name>[\w]+).+VERSION\s+(?P<version>[\d\.*]+)", flags=re.I | re.S)
    content = load(recipe, "CMakeLists.txt")
    match = pattern.search(content)
    
    return match.group('name'), match.group('version')

class Recipe(ConanFile):
    def __init__(self, display_name):
        super().__init__(display_name)

        # Read name and version from CMake
        self.CMAKE_PROJECT_NAME, self.version = ReadNameVersionFromCmake(self)
        self.name = self.CMAKE_PROJECT_NAME.lower()
        self.package_type = "shared-library"

    # Binary configuration
    settings = "os", "compiler", "build_type", "arch"

    # Sources are located in the same place as this recipe, copy them to the recipe
    exports_sources = [
        "CMakeLists.txt",
        "CMakeSettings.json",   
        "src/*",
        "gui/*",
        "samples/*",
        "tests/*",
        "tools/*"]

    def config_options(self):
        pass

    def configure(self):
        pass

    def layout(self):
        cmake_layout(self)

    def generate(self):
        deps = CMakeDeps(self)
        deps.generate()
        tc = CMakeToolchain(self, generator="Ninja Multi-Config")
        tc.generate()

    def build(self):
        cmake = CMake(self)
        cmake.configure()
        cmake.build()

    def package(self):
        cmake = CMake(self)
        cmake.install()

    def package_info(self):
        self.cpp_info.libs = collect_libs(self)


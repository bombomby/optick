cmake_minimum_required(VERSION 3.2)

project(PackageTest CXX)

find_package(optick CONFIG REQUIRED)

add_executable(${CMAKE_PROJECT_NAME} src/main.cpp)

target_link_libraries(${CMAKE_PROJECT_NAME} optick::optick)

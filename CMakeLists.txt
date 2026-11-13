cmake_minimum_required(VERSION 3.20)
project(MistIsle C)
set(CMAKE_C_STANDARD 11)
set(CMAKE_C_STANDARD_REQUIRED ON)

option(MISTISLE_PI_PROFILE "Build the Raspberry Pi 5 compatibility/performance profile" OFF)
find_package(Python3 REQUIRED COMPONENTS Interpreter)

# raylib 5.5 uses OpenGL 3.3 by default. Raspberry Pi OS desktop builds use
# the conservative OpenGL 2.1 backend for broad Mesa/X11 compatibility.
if(MISTISLE_PI_PROFILE)
  set(PLATFORM "Desktop" CACHE STRING "" FORCE)
  set(OPENGL_VERSION "2.1" CACHE STRING "" FORCE)
  set(GLFW_BUILD_X11 ON CACHE BOOL "" FORCE)
  set(GLFW_BUILD_WAYLAND ON CACHE BOOL "" FORCE)
  add_compile_definitions(MISTISLE_PI_PROFILE=1)
else()
  set(PLATFORM "Desktop" CACHE STRING "" FORCE)
  set(OPENGL_VERSION "3.3" CACHE STRING "" FORCE)
endif()

include(FetchContent)
set(BUILD_EXAMPLES OFF CACHE BOOL "" FORCE)
set(BUILD_GAMES OFF CACHE BOOL "" FORCE)
FetchContent_Declare(raylib GIT_REPOSITORY https://github.com/raysan5/raylib.git GIT_TAG 5.5)
FetchContent_MakeAvailable(raylib)

set(MISTISLE_GENERATED_DIR "${CMAKE_CURRENT_BINARY_DIR}/generated")
set(MISTISLE_MAP_HEADER "${MISTISLE_GENERATED_DIR}/area_maps.h")
add_custom_command(
  OUTPUT "${MISTISLE_MAP_HEADER}"
  COMMAND "${Python3_EXECUTABLE}" "${CMAKE_SOURCE_DIR}/tools/build_maps.py" --output "${MISTISLE_MAP_HEADER}"
  DEPENDS
    "${CMAKE_SOURCE_DIR}/tools/build_maps.py"
    "${CMAKE_SOURCE_DIR}/tools/map_autotile.py"
    "${CMAKE_SOURCE_DIR}/maps/terrain.tsx"
    "${CMAKE_SOURCE_DIR}/maps/coastal_camp.tmx"
    "${CMAKE_SOURCE_DIR}/maps/glowglade_grove.tmx"
    "${CMAKE_SOURCE_DIR}/maps/tidal_ruins.tmx"
    "${CMAKE_SOURCE_DIR}/maps/lighthouse_courtyard.tmx"
  VERBATIM
)
add_custom_target(generate_area_maps DEPENDS "${MISTISLE_MAP_HEADER}")

add_executable(mist_isle
  src/main.c
  src/game.c
  src/render.c
  "${MISTISLE_MAP_HEADER}"
)
add_dependencies(mist_isle generate_area_maps)
target_include_directories(mist_isle PRIVATE src "${MISTISLE_GENERATED_DIR}")
target_link_libraries(mist_isle PRIVATE raylib)
if(MSVC)
  target_compile_options(mist_isle PRIVATE /utf-8)
endif()
set_property(TARGET mist_isle PROPERTY VS_DEBUGGER_WORKING_DIRECTORY "${CMAKE_SOURCE_DIR}")
if(UNIX AND NOT APPLE)
  target_link_libraries(mist_isle PRIVATE m pthread dl rt)
endif()

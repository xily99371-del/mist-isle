cmake_minimum_required(VERSION 3.20)
project(MistIsle C)

# Unpack the verified native-resolution pixel art bundle at configure time.
set(MIST_PIXEL_ARCHIVE "${CMAKE_SOURCE_DIR}/assets/MistIsle_BinaryAssets_v2.zip")
if(EXISTS "${MIST_PIXEL_ARCHIVE}")
  file(ARCHIVE_EXTRACT INPUT "${MIST_PIXEL_ARCHIVE}" DESTINATION "${CMAKE_SOURCE_DIR}")
endif()
set(CMAKE_C_STANDARD 11)
set(CMAKE_C_STANDARD_REQUIRED ON)
include(FetchContent)
set(BUILD_EXAMPLES OFF CACHE BOOL "" FORCE)
set(BUILD_GAMES OFF CACHE BOOL "" FORCE)
FetchContent_Declare(raylib
  GIT_REPOSITORY https://github.com/raysan5/raylib.git
  GIT_TAG 5.5
)
FetchContent_MakeAvailable(raylib)
add_executable(mist_isle src/main.c)
target_link_libraries(mist_isle PRIVATE raylib)
set_property(TARGET mist_isle PROPERTY VS_DEBUGGER_WORKING_DIRECTORY "${CMAKE_SOURCE_DIR}")
if(UNIX AND NOT APPLE)
  target_link_libraries(mist_isle PRIVATE m pthread dl rt)
endif()
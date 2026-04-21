#!/usr/bin/env python
import os
import sys

env = SConscript("godot-cpp/SConstruct")

# For reference:
# - CCFLAGS are compilation flags shared between C and C++
# - CFLAGS are for C-specific compilation flags
# - CXXFLAGS are for C++-specific compilation flags
# - CPPFLAGS are for pre-processor flags
# - CPPDEFINES are for pre-processor defines
# - LINKFLAGS are for linking flags

# tweak this if you want to use different folders, or more folders, to store your source code in.
env.Append(CPPPATH=["source-cpp/", "GraphDraw/", "Utilities/", "EQ/", "Measurement/"])
sources = [Glob("source-cpp/*.cpp"), Glob("source-cpp/GraphDraw/*.cpp"), Glob("source-cpp/Utilities/*.cpp"), Glob("source-cpp/EQ/*.cpp"), Glob("source-cpp/Measurement/*.cpp")]

if env["platform"] == "macos":
    library = env.SharedLibrary(
        "MEQ/bin/libmeq.{}.{}.framework/libgdexample.{}.{}".format(
            env["platform"], env["target"], env["platform"], env["target"]
        ),
        source=sources,
    )
else:
    library = env.SharedLibrary(
        "MEQ/bin/libmeq{}{}".format(env["suffix"], env["SHLIBSUFFIX"]),
        source=sources,
    )

Default(library)

#!/usr/bin/env python3
"""Regression checks for rendered building foundations and map reachability."""
from pathlib import Path
import sys
ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT/"tools"))
from build_maps import parse, W

def check(map_name, center_x, ground_y, half_width, label):
    _, collision, _, _, _ = parse(ROOT/"maps"/(map_name+".tmx"))
    for x in range(center_x-half_width,center_x+half_width+1):
        assert collision[ground_y*W+x], f"{label}: missing collision at {(x,ground_y)}"
    print(f"COLLISION OK: {label} foundation")

check("coastal_camp",11,3,2,"Coastal Keeper House")
check("lighthouse_courtyard",9,3,1,"Lost Lighthouse")

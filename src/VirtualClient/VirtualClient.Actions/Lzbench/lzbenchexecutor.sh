#!/bin/bash

echo $1
# Use stdbuf to force line-buffered stdout so CSV data is flushed incrementally
# to disk rather than held in a full-buffer until process exit. Without this,
# a timeout-based kill loses all buffered output (especially on ARM64).
stdbuf -oL ./lzbench $1 > results-summary.csv


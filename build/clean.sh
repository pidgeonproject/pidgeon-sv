#!/bin/sh
force=0

if [ x"$1" = x"--force" ];then
  force=1
fi

if [ -d obj ];then
  rm -rv obj
fi

if [ -d bin ];then
if [ $force -eq 0 ];then
  if [ -d bin/Debug/data ] || [ -d bin/Release/data ];then
    echo "Not removing bin because there is a data folder, use make forced-clean if you really want to delete it"
    exit 1
  fi
fi

rm -rv bin
fi


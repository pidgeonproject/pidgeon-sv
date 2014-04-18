#!/bin/bash

if [ "$#" -lt 1 ];then
    echo "Enter version"
    read v
else
    v="$1"
fi

temp=/tmp/pidgeon-sv_"$v"

if [ -d "$temp" ];then
  echo "Remove $temp or package will not be created"
  exit 1
fi

if [ -f "$temp.orig.tar.gz" ];then
  echo "Remove $temp or package will not be created"
  exit 1
fi

mkdir "$temp" || exit 1
cp -vr src/* "$temp" || exit 1
cd /tmp || exit 1
tar -zcf "pidgeon-sv_"$v".orig.tar.gz" "$temp"
cd "$temp"
echo "Press enter to execute debuild"
read pause
debuild


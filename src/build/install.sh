#!/bin/bash
#  Copyright (c) 2013, Petr Bena
#  All rights reserved.

#  Redistribution and use in source and binary forms, with
#  or without modification, are permitted provided that
#  the following conditions are met:

#  1. Redistributions of source code must retain
#     the above copyright notice, this list 
#     of conditions and the following disclaimer.

#  2. Redistributions in binary form must reproduce the above copyright
#     notice, this list of conditions and the following disclaimer in
#     the documentation and/or other materials provided with the distribution.

#  3. Neither the name of Huggle nor the names of its contributors may be used
#     to endorse or promote products derived from this software without specific
#     prior written permission.

#  THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS
#  OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF
#  MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL
#  THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
#  SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT
#  OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION)
#  HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
#  OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE,
#  EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

DEST=/
TARGET=/opt/pidgeon-sv

if [ "$#" -gt 0 ];then
  if [ x"$1" != x"" ];then
    DEST=$1/
  fi
fi

echo "Installing pidgeon-sv to $DEST/$TARGET"
if [ ! -d "$DEST"$TARGET ];then
  mkdir -p "$DEST"$TARGET || exit 1
fi
if [ ! -d "$DEST"usr/bin/ ];then
  mkdir -p "$DEST"usr/bin/ || exit 1
fi
if [ ! -d "$DEST"usr/share/man/man1 ];then
  mkdir -p "$DEST"usr/share/man/man1 || exit 1
fi
if [ ! -d "$DEST"etc/init.d/ ];then
  mkdir -p "$DEST"etc/init.d/ || exit 1
fi

cp -v build/pidgeon-sv "$DEST"/usr/bin || exit 1
cp -v build/service "$DEST"/etc/init.d || exit 1
cp -v bin/Debug/*.dll "$DEST"$TARGET/ || exit 1
cp -v bin/Debug/*.exe "$DEST"$TARGET/ || exit 1


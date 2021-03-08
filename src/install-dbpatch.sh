#!/usr/bin/env bash
# Copyright (c) Zack Moore MIT License. All rights reserved.
# https://github.com/ormico/dbpatchmanager
##################################################################
#you can test using the docker dotnet container image
#docker run -it --rm mcr.microsoft.com/dotnet/runtime /bin/bash
##################################################################
#dependencies
#apt-get update && apt-get install -y wget unzip
##################################################################
{
    INSTALL_DIR="/usr/local/lib/dbpatch"
    BIN="/usr/local/bin/dbpatch"

    if [ -h $BIN ]; then
        echo "=> remove old symlink $BIN"
        rm $BIN
    fi

    if [ -f $BIN ]; then
        echo "=> there is a file at $BIN. install cannot proceed."
        exit 1
    fi

    if [ -d $INSTALL_DIR ]; then
        echo "=> dbpatch is already installed in $INSTALL_DIR, trying to update"
        rm -r $INSTALL_DIR
    fi

    mkdir $INSTALL_DIR
    # make release zip file dbpatch.zip . don't put version in filename. that way installer won't have to be updated for each version
    #wget https://github.com/ormico/dbpatchmanager/releases/download/v2.0.127/dbpatch-v1.0.127.zip -O "$INSTALL_DIR/dbpatch.zip"
    #wget https://github.com/ormico/dbpatchmanager/releases/download/v2.0.127/dbpatch.zip -O "$INSTALL_DIR/dbpatch.zip"
    wget https://github.com/ormico/dbpatchmanager/releases/latest/download/dbpatch.zip -O /usr/local/lib/dbpatch/dbpatch.zip
    unzip "$INSTALL_DIR/dbpatch.zip" -d $INSTALL_DIR
    chmod +x "$INSTALL_DIR/dbpatch"
    rm "$INSTALL_DIR/dbpatch.zip"

    # make sure file is in linux format so you don't need this dependency
    #dos2unix /usr/local/lib/dbpatch/dbpatch
    ln -s "$INSTALL_DIR/dbpatch" $BIN
}

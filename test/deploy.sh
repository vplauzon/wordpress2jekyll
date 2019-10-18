#!/bin/bash

###################################################################################################
### Deploy deploy.json
###
### Inputs:
###     Name of the resource group
###     Name of the storage account

#   Bind script parameters
rg=$1
storageAccountName=$2

echo "Resource Group Name:  $rg"
echo "Storage Account Name:  $storageAccountName"

#   Create unique ID for deployment name
uniqueId=$(uuidgen)
name="deploy-$uniqueId"

az group deployment create -n $name -g $rg --template-file deploy.json \
    --parameters storageAccountName=$storageAccountName
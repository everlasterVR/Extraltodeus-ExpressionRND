#!/bin/bash

files=$(grep -o '<Compile Include="src\\[^"]*"' Extraltodeus-ExpressionRND.csproj | sed 's/<Compile Include="//; s/"//')
echo "$files" > ExpressionRandomizer.cslist

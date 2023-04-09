#!/bin/bash

set -e

author_name=VamTimbo
resource_name=Extraltodeus-ExpressionRND

package_version=$1
[ -z "$package_version" ] && printf "Usage: ./package.sh [var package version]\n" && exit 1

plugin_version=$(git describe --tags --match "v*" --abbrev=0 HEAD 2>/dev/null | sed s/v//)
[ -z "$plugin_version" ] && printf "Git tag not set on current commit.\n" && exit 1

# packaging: main
work_dir=publish
mkdir -p work_dir

resource_dir="$work_dir/Custom/Scripts/__Frequently Used"
mkdir -p "$resource_dir"
cp meta.json "$work_dir/"
cp ExpressionRandomizer_1.7_Timbo.cs "$resource_dir/"

# zip files to .var and cleanup
printf "Creating package...\n"
package_file="$author_name.$resource_name.$package_version.var"
cd $work_dir
zip -rq "$package_file" ./*
printf "Package %s created for plugin version v%s.\n" "$package_file" "$plugin_version"
mv "$package_file" ..
cd ..
rm -rf $work_dir

# move archive to AddonPackages
addon_packages_dir=../../../../AddonPackages
mkdir -p $addon_packages_dir
mv "$package_file" $addon_packages_dir
printf "Package %s moved to AddonPackages.\n" "$package_file"

#!/bin/bash

set -e

expression_randomizer=ExpressionRandomizer

package_version=$1
[ -z "$package_version" ] && printf "Usage: ./package.sh [var package version]\n" && exit 1

plugin_version=$(git describe --tags --match "v*" --abbrev=0 HEAD 2>/dev/null | sed s/v//)
[ -z "$plugin_version" ] && printf "Git tag not set on current commit.\n" && exit 1

# packaging
work_dir=publish
mkdir -p "$work_dir"
cp meta.json "$work_dir"/

resource_dir="$work_dir/Custom/Scripts/tmp"
mkdir -p "$resource_dir"
cp "$expression_randomizer"_1.7_Timbo.cs "$resource_dir"/
cp "$expression_randomizer".cslist "$resource_dir"/
cp -r src "$resource_dir"/

# update version info
sed -i "s/0\.0\.0/$plugin_version/g" "$work_dir"/meta.json
sed -i "s/0\.0\.0/$plugin_version/g" "$resource_dir"/src/$expression_randomizer.cs

for file in $(find "$resource_dir" -type f -name "*.cs"); do
    # set production env
    sed -i "s/#define ENV_DEVELOPMENT/\/\//" "$file"
    # hide .cs files (plugin is loaded with .cslist)
    touch "$file".hide
done

mv "$work_dir/Custom/Scripts/tmp" "$work_dir/Custom/Scripts/__Frequently Used"

# zip files to .var and cleanup
printf "Creating package...\n"
package_file="VamTimbo.Extraltodeus-ExpressionRND.$package_version.var"
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

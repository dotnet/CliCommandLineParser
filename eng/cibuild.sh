#!/usr/bin/env bash

source="${BASH_SOURCE[0]}"

# resolve $SOURCE until the file is no longer a symlink
while [[ -h $source ]]; do
  scriptroot="$( cd -P "$( dirname "$source" )" && pwd )"
  source="$(readlink "$source")"

  # if $source was a relative symlink, we need to resolve it relative to the path where 
  # the symlink file was located
  [[ $source != /* ]] && source="$scriptroot/$source"
done
scriptroot="$( cd -P "$( dirname "$source" )" && pwd )"

# install the 2.1.0 runtime for running tests
"$scriptroot/common/dotnet-install.sh"  -runtime dotnet -version 2.1.0

. "$scriptroot/common/cibuild.sh" --restore --build --test --pack --publish --ci $@

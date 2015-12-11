@echo off

set DEST=nuget\lib\net45

rmdir /S /Q %DEST% 2> NUL
mkdir %DEST%

copy bin\Release\RSB.dll %DEST%
copy bin\Release\RSB.pdb %DEST%

pushd nuget
nuget pack
popd
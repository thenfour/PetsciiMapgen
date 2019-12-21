@echo off
pushd %~dp0..

bin\Release\PetsciiMapgen.exe -batchrun emojidark budget
bin\Release\PetsciiMapgen.exe -batchrun c64 budget
bin\Release\PetsciiMapgen.exe -batchrun mz700 budget
bin\Release\PetsciiMapgen.exe -batchrun topaz budget
bin\Release\PetsciiMapgen.exe -batchrun vga budget

popd

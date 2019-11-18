@echo off
pushd %~dp0..

bin\Release\PetsciiMapgen.exe -batchrun emojidark heavy
bin\Release\PetsciiMapgen.exe -batchrun c64 heavy
bin\Release\PetsciiMapgen.exe -batchrun mz700 heavy
bin\Release\PetsciiMapgen.exe -batchrun topaz heavy
bin\Release\PetsciiMapgen.exe -batchrun vga heavy

popd

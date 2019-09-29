@echo off
pushd %~dp0..


bin\Release\PetsciiMapgen.exe -batchrun emojidark 12x12 medium
bin\Release\PetsciiMapgen.exe -batchrun emojidark 12x12 heavy

bin\Release\PetsciiMapgen.exe -batchrun emojidark 16x16 medium
bin\Release\PetsciiMapgen.exe -batchrun emojidark 16x16 heavy

bin\Release\PetsciiMapgen.exe -batchrun c64 medium
bin\Release\PetsciiMapgen.exe -batchrun c64 heavy

bin\Release\PetsciiMapgen.exe -batchrun mz700 medium
bin\Release\PetsciiMapgen.exe -batchrun mz700 heavy

popd
@echo off

"C:\Program Files\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\msbuild" "..\src\CommandLine.sln" /t:Build /p:Configuration=Debug /v:m /m

"..\src\CommandLine\bin\Debug\net461\roslynator" format "..\src\Roslynator.sln" ^
  --log-file "roslynator.log" ^
  --log-file-verbosity d ^
  --verbosity n ^

pause

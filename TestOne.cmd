start TestService\bin\debug\net8.0\OtelTestService.exe --Urls http://localhost:5000 --OTLP_ENDPOINT_URL https://localhost:4317

:loop
curl http://localhost:5000/NestedGreeting?nestlevel=5
choice /m "Abort?" /c yn /t 1 /d n
goto answer%errorlevel%

:answer2
goto loop

:answer1
taskkill /F /IM OtelTestService.exe


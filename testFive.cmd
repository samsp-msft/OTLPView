start TestService\bin\debug\net8.0\OtelTestService.exe --Urls http://localhost:5000 --OTLP_ENDPOINT_URL https://localhost:4317 --TEST_CMD true
start TestService\bin\debug\net8.0\OtelTestService.exe --Urls http://localhost:5001 --OTLP_ENDPOINT_URL https://localhost:4317 --TEST_CMD true
start TestService\bin\debug\net8.0\OtelTestService.exe --Urls http://localhost:5002 --OTLP_ENDPOINT_URL https://localhost:4317 --TEST_CMD true
start TestService\bin\debug\net8.0\OtelTestService.exe --Urls http://localhost:5003 --OTLP_ENDPOINT_URL https://localhost:4317 --TEST_CMD true
start TestService\bin\debug\net8.0\OtelTestService.exe --Urls http://localhost:5004 --OTLP_ENDPOINT_URL https://localhost:4317 --TEST_CMD true

timeout /t3

:loop
curl http://localhost:5000/NestedGreeting?nestlevel=5
curl http://localhost:5001/NestedGreeting?nestlevel=5
curl http://localhost:5002/NestedGreeting?nestlevel=5
curl http://localhost:5003/NestedGreeting?nestlevel=5
curl http://localhost:5004/NestedGreeting?nestlevel=5
choice /m "Abort?" /c yn /t 5 /d n
goto answer%errorlevel%

:answer2
goto loop

:answer1
taskkill /F /IM OtelTestService.exe


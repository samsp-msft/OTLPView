# OTLPView

## An OTLP Viewer written in .NET Blazor

Provides a viewer for telemetry emitted via OTLP. Implements an OTLP/gRPC endpoint that you can point other applications at to collect and analyze the data that is being emitted.

## Usage

Run the application to create an OTLP sync endpoint on port 4317. Point applications that you wish to monitor at that port, and it will collect the OTLP data, and present it in a human readable form

- Log Viewer - shows logs emitted with semantic logging so the individal parameters are extracted from the logging payload.
- Metrics - see the metrics that are being exported and updated by the application
- Distributed Tracing - track individual operations as they are processed across multiple nodes in the system

## Test application
Use the included app to send telemetery from its two endpoints
test.cmd will launch 5 instances and cause requests to bounce amongst them

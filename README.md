# productionProxyPLCtoMES
This is a simple proxy program that stands in between a PLC message and a GHP MES app.
Please note that all ports needs to be adjusted to your specific needs.
 
productionProxyPLCtoMES is a lightweight, fault‑tolerant TCP proxy designed to sit transparently between a PLC and a GHP MES.
Its primary purpose is to intercept, validate, and sanitize messages before they reach MES, preventing malformed or unexpected PLC traffic from causing MES crashes or production downtime.

The proxy forwards all valid messages to the MES endpoint and returns MES responses back to the PLC, acting as a safe middleware layer.
It includes:

Configurable ports and endpoints (PLC → Proxy → MES)

Message filtering and validation to block malformed or dangerous traffic

Robust logging for diagnostics, auditing, and troubleshooting

Graceful error handling to keep production stable even when PLC messages are inconsistent

This tool is intended for production environments where PLC‑to‑MES communication must be protected, monitored, and made more resilient.

Diagram:
 ┌──────────────┐        ┌────────────────────┐        ┌──────────────┐
 │     PLC       │ -----> │   productionProxy   │ -----> │     MES       │
 │ (Machine Side)│        │ (This Application)  │        │   (GHP App)   │
 └──────────────┘ <----- └────────────────────┘ <----- └──────────────┘
         ▲                        ▲                          ▲
         │                        │                          │
         │                        │                          │
         └──────────── Logs & Diagnostics ───────────────────┘

The PLC must point to the proxy’s listening port.

The proxy forwards messages to the MES port.

All ports and IPs must be adjusted to your environment.

Logging is enabled by default for troubleshooting.

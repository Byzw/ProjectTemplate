@echo off
cd /d %~dp0
start "" ngrok.exe http 5566
exit
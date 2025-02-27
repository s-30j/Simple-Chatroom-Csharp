const WebSocket = require('ws');
const fs = require('fs');
const path = require('path');

const PORT = 8080;
const LOG_FILE = path.join(__dirname, 'chat_logs.txt');

const wss = new WebSocket.Server({ port: PORT });

const connections = new Map();

function logMessage(message) {
    const timestamp = new Date().toISOString();
    const logEntry = `${timestamp} - ${message}\n`;
    fs.appendFileSync(LOG_FILE, logEntry);
}

wss.on('connection', (ws, req) => {
    const clientIp = req.socket.remoteAddress;
    
    ws.on('message', (data) => {
        try {
            const message = JSON.parse(data);
            
            if (message.type === 'identify') {
                const userId = message.userId;
                connections.set(userId, { ws, ip: clientIp });
                logMessage(`User Connected - ID: ${userId}, IP: ${clientIp}`);
                
                ws.send(JSON.stringify({
                    type: 'connected',
                    userId: userId
                }));
            }
            else if (message.type === 'chat') {
                const timestamp = new Date().toISOString();
                logMessage(`Message from ${message.userId}: ${message.content}`);
                
                connections.forEach((connection) => {
                    if (connection.ws.readyState === WebSocket.OPEN) {
                        connection.ws.send(JSON.stringify({
                            type: 'chat',
                            userId: message.userId,
                            content: message.content,
                            timestamp: timestamp
                        }));
                    }
                });
            }
        } catch (error) {
            logMessage(`Error processing message: ${error.message}`);
        }
    });

    ws.on('close', () => {
        for (const [userId, connection] of connections.entries()) {
            if (connection.ws === ws) {
                logMessage(`User Disconnected - ID: ${userId}, IP: ${clientIp}`);
                connections.delete(userId);
                break;
            }
        }
    });

    ws.on('error', (error) => {
        logMessage(`WebSocket Error: ${error.message}`);
    });
});

console.log(`WebSocket server running on port ${PORT}`);
logMessage(`Server started on port ${PORT}`);

import express from 'express';
import cors from 'cors';
import fs from 'fs/promises';
import path from 'path';
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const app = express();
const PORT = 3001;

app.use(cors());
app.use(express.json());

const COMMANDS_FILE = path.join(__dirname, 'public', 'commands.json');

// Endpoint to append a command
app.post('/api/command', async (req, res) => {
    try {
        const { type, payload } = req.body;

        if (!type) {
            return res.status(400).json({ error: 'Command type is required' });
        }

        // Read existing
        let currentData = { commands: [] };
        try {
            const data = await fs.readFile(COMMANDS_FILE, 'utf8');
            currentData = JSON.parse(data);
        } catch (e) {
            // File might not exist or be empty, ignore
        }

        const newCommand = {
            id: Date.now().toString(),
            type,
            payload: payload || {},
            timestamp: Date.now()
        };

        currentData.commands.push(newCommand);

        await fs.writeFile(COMMANDS_FILE, JSON.stringify(currentData, null, 2));

        console.log(`[API] Command written: ${type}`);
        res.json({ success: true, command: newCommand });

    } catch (error) {
        console.error('Error writing command:', error);
        res.status(500).json({ error: 'Internal Server Error' });
    }
});

// Endpoint to clear commands (optional, helper)
app.post('/api/clear-commands', async (req, res) => {
    try {
        await fs.writeFile(COMMANDS_FILE, JSON.stringify({ commands: [] }, null, 2));
        res.json({ success: true });
    } catch (error) {
        res.status(500).json({ error: 'Failed to clear commands' });
    }
});

app.listen(PORT, () => {
    console.log(`Retentia Command API running on http://localhost:${PORT}`);
});

const fs   = require('fs');
const path = require('path');

const assetsBase = path.join(__dirname, 'Assets');

const folders = [
    path.join(assetsBase, 'MiniGames'),
    path.join(assetsBase, 'Resources', 'MiniGames'),
    path.join(assetsBase, 'Scripts'),
];

folders.forEach(dir => {
    if (!fs.existsSync(dir)) {
        fs.mkdirSync(dir, { recursive: true });
        console.log(`Created: ${dir}`);
    } else {
        console.log(`Exists:  ${dir}`);
    }
});

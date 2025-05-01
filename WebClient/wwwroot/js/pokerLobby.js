let connection;
let dotnetRefGlobal;

export async function startConnection(tableId, nickname, dotnetRef, receiveCallback, joinCallback) {
    dotnetRefGlobal = dotnetRef;
    connection = new signalR.HubConnectionBuilder()
        .withUrl("http://localhost:5005/pokerhub")
        .build();

    connection.on("ReceiveTableMessage", function (sender, message, timestamp) {
        dotnetRef.invokeMethodAsync("AddChatMessage", sender, message, timestamp);
    });

    connection.on("TableJoinFailed", async (tableId, reason) => {
        await dotnetRef.invokeMethodAsync("OnJoinFailed", tableId, reason);
        await connection.stop(); // clean up
    });

    connection.on("PlayerJoined", (playerName) => {
        console.log("Player joined:", playerName);
        dotnetRef.invokeMethodAsync(joinCallback, playerName);
    });

    await connection.start();
    await connection.invoke("JoinTable", tableId, nickname);
}

export async function sendMessage(tableId, sender, message) {
    if (connection) {
        await connection.invoke("SendMessage", tableId, sender, message);
    }
}

export async function sendPlayerAction(tableId, playerName, action, raiseAmount = 0) {
    if (connection) {
        await connection.invoke("PlayerAction", tableId, playerName, action, raiseAmount);
    }
}

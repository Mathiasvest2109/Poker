let connection;

export async function startConnection(tableId, nickname, dotnetRef, receiveCallback, joinCallback) {
    connection = new signalR.HubConnectionBuilder()
        .withUrl("http://localhost:5005/pokerhub")
        .build();

    connection.on("ReceiveTableMessage", (user, message, timestamp) => {
        console.log("Message received:", user, message, timestamp);
        dotnetRef.invokeMethodAsync(receiveCallback, user, message, timestamp);
    });

    connection.on("PlayerJoined", (playerName) => {
        console.log("👤 Player joined:", playerName);
        dotnetRef.invokeMethodAsync(joinCallback, playerName);
    });

    await connection.start();
    await connection.invoke("JoinTable", tableId, nickname);
}

export async function sendMessage(tableId, message) {
    if (connection) {
        await connection.invoke("SendTableMessage", tableId, message);
    }
}

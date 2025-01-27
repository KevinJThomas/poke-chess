import BattleBoard from "./components/BattleBoard";
import Button from "./components/Button";
import Gold from "./components/Gold";
import Players from "./components/Opponents";
import ShopBoard from "./components/ShopBoard";
import { DragDropContext } from "@hello-pangea/dnd";
import { useEffect, useState } from "react";
import * as signalR from "@microsoft/signalr";
import Error from "./components/Error";
import Lobby from "./components/pages/LobbyPage";
import NamePage from "./components/pages/NamePage";

export default function App() {
  const [gameStatus, setGameStatus] = useState("name");
  const [connection, setConnection] = useState();
  const [error, setError] = useState();
  const [players, setPlayers] = useState([]);
  const [name, setName] = useState("");
  const [gameState, setGameState] = useState();
  const [playerId, setPlayerId] = useState();

  function onDragEnd(result) {
    console.log("onDragEnd", result);
  }

  useEffect(() => {
    const url =
      import.meta.env.MODE === "development"
        ? "http://localhost:5043/messageHub"
        : "/messageHub";

    const connection = new signalR.HubConnectionBuilder()
      .configureLogging(signalR.LogLevel.Debug) // add this for diagnostic clues
      .withUrl(url, {
        skipNegotiation: true, // skipNegotiation as we specify WebSockets
        transport: signalR.HttpTransportType.WebSockets, // force WebSocket transport
      })
      .build();

    connection.start().catch((error) => {
      setError(error);
      setGameStatus("error");
    });

    setConnection(connection);
  }, []);

  useEffect(() => {
    if (!connection) {
      return;
    }

    connection.on("LobbyUpdated", (lobby, playerId) => {
      if (playerId) {
        setPlayerId(playerId);
      }
      setGameStatus("lobby");
      setPlayers(lobby.players);
    });

    connection.on("GameError", (error) => {
      setError(error);
      setGameStatus("error");
    });

    connection.on("StartGameConfirmed", (lobby) => {
      setPlayers(lobby.players);
      setGameState(lobby.gameState);
      setGameStatus("shop");
    });

    return () => {
      connection.off("LobbyUpdated");
      connection.off("GameError");
      connection.off("StartGameConfirmed");
    };
  }, [connection]);

  if (gameStatus === "error") {
    return <Error error={error} />;
  }

  if (gameStatus === "lobby") {
    return <Lobby players={players} connection={connection} />;
  }

  if (gameStatus === "name") {
    return (
      <NamePage
        connection={connection}
        setGameStatus={setGameStatus}
        setError={setError}
        name={name}
        setName={setName}
      />
    );
  }

  return (
    <DragDropContext onDragEnd={onDragEnd}>
      <div className="relative h-screen w-screen bg-gray-200">
        {gameStatus === "shop" && (
          <ShopBoard
            connection={connection}
            players={players}
            gameState={gameState}
            playerId={playerId}
          />
        )}
        {gameStatus === "battle" && <BattleBoard />}
        <Button className="absolute top-1/2 right-0">End Turn</Button>
        <Gold gold={0} maxGold={5} />
        <Players players={players} />
      </div>
    </DragDropContext>
  );
}

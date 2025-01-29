import BattleBoard from "./components/BattleBoard";
import Button from "./components/Button";
import Gold from "./components/Gold";
import Opponents from "./components/Opponents";
import ShopBoard from "./components/ShopBoard";
import { DragDropContext } from "@hello-pangea/dnd";
import { useEffect, useState } from "react";
import * as signalR from "@microsoft/signalr";
import Error from "./components/Error";
import Lobby from "./components/pages/LobbyPage";
import NamePage from "./components/pages/NamePage";
import cloneDeep from "lodash/cloneDeep";

export default function App() {
  const [gameStatus, setGameStatus] = useState("name");
  const [connection, setConnection] = useState();
  const [error, setError] = useState();
  const [players, setPlayers] = useState([]);
  const [name, setName] = useState("");
  const [gameState, setGameState] = useState();
  const [playerId, setPlayerId] = useState();
  const [disableSellDrop, setDisableSellDrop] = useState(false);
  const [disableBoardDrop, setDisableBoardDrop] = useState(false);
  const [disableShopDrop, setDisableShopDrop] = useState(false);
  const [disableHandDrop, setDisableHandDrop] = useState(false);
  const [hasEndedTurn, setHasEndedTurn] = useState(false);

  console.log("players", players);
  console.log("gameState", gameState);

  const player = players.find((player) => player.id === playerId);

  function onDragStart(result) {
    if (result.source.droppableId === "droppable-shop") {
      setDisableSellDrop(true);
      setDisableBoardDrop(true);
    }

    if (result.source.droppableId === "droppable-hand") {
      setDisableShopDrop(true);
    }

    if (result.source.droppableId === "droppable-board") {
      setDisableShopDrop(true);
      setDisableHandDrop(true);
    }
  }

  function onDragEnd(result) {
    setDisableSellDrop(false);
    setDisableBoardDrop(false);
    setDisableShopDrop(false);
    setDisableHandDrop(false);
    // Check if the drag was canceled
    if (!result.destination) return;

    const cardId = result.draggableId;

    // Sell
    if (
      result.source.droppableId === "droppable-board" &&
      result.destination.droppableId === "droppable-sell"
    ) {
      const clonedPlayers = cloneDeep(players);

      const playerIndex = clonedPlayers.findIndex(
        (player) => player.id === playerId,
      );

      clonedPlayers[playerIndex].board = clonedPlayers[
        playerIndex
      ].board.filter((card) => card.id !== cardId);

      setPlayers(clonedPlayers);

      connection.invoke("MoveCard", result.draggableId, 1, null, null);
    }

    // Play
    if (
      result.source.droppableId === "droppable-hand" &&
      result.destination.droppableId === "droppable-board"
    ) {
      const clonedPlayers = cloneDeep(players);

      const playerIndex = clonedPlayers.findIndex(
        (player) => player.id === playerId,
      );

      const card = clonedPlayers[playerIndex].hand.find(
        (card) => card.id === cardId,
      );

      clonedPlayers[playerIndex].hand = clonedPlayers[playerIndex].hand.filter(
        (card) => card.id !== cardId,
      );

      clonedPlayers[playerIndex].board.splice(
        result.destination.index,
        0,
        card,
      );

      setPlayers(clonedPlayers);

      connection.invoke(
        "MoveCard",
        result.draggableId,
        2,
        result.destination.index,
        null,
      );
    }

    // Buy
    if (
      result.source.droppableId === "droppable-shop" &&
      result.destination.droppableId === "droppable-hand"
    ) {
      const clonedPlayers = cloneDeep(players);

      const playerIndex = clonedPlayers.findIndex(
        (player) => player.id === playerId,
      );

      const card = clonedPlayers[playerIndex].shop.find(
        (card) => card.id === cardId,
      );

      clonedPlayers[playerIndex].shop = clonedPlayers[playerIndex].shop.filter(
        (card) => card.id !== cardId,
      );

      clonedPlayers[playerIndex].hand.push(card);

      setPlayers(clonedPlayers);

      connection.invoke("MoveCard", result.draggableId, 0, null, null);
    }
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
      if (lobby.isWaitingToStart) {
        setGameStatus("lobby");
      }
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

    connection.on("PlayerUpdated", (newPlayer) => {
      setPlayers((prev) =>
        prev.map((player) => {
          if (player.id === playerId) {
            return newPlayer;
          }

          return player;
        }),
      );
    });

    connection.on("CombatComplete", (x) => {
      console.log("CombatComplete", x);
    });

    return () => {
      connection.off("LobbyUpdated");
      connection.off("GameError");
      connection.off("StartGameConfirmed");
      connection.off("PlayerUpdated");
    };
  }, [connection, playerId]);

  function endTurn() {
    connection.invoke("EndTurn");
    setHasEndedTurn(true);
  }

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
    <DragDropContext onDragEnd={onDragEnd} onDragStart={onDragStart}>
      {hasEndedTurn && (
        <div className="absolute top-0 right-0 bottom-0 left-0 z-10 flex items-center justify-center bg-black/50">
          <span className="rounded-md bg-gray-300 p-2">
            Waiting for other players
          </span>
        </div>
      )}
      <div className="relative h-screen w-screen bg-gray-200">
        {gameStatus === "shop" && (
          <ShopBoard
            connection={connection}
            players={players}
            gameState={gameState}
            player={player}
            disableSellDrop={disableSellDrop}
            disableBoardDrop={disableBoardDrop}
            disableShopDrop={disableShopDrop}
            disableHandDrop={disableHandDrop}
          />
        )}
        {gameStatus === "battle" && <BattleBoard />}
        <Button
          className="absolute top-1/2 right-0"
          onClick={endTurn}
          disabled={hasEndedTurn}
        >
          End Turn
        </Button>
        <Gold gold={player.gold} maxGold={player.baseGold} />
        <Opponents players={players} />
      </div>
    </DragDropContext>
  );
}

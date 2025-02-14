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
import GameOverPage from "./components/pages/GameOverPage";

export default function App() {
  const [gameStatus, setGameStatus] = useState("name");
  const [connection, setConnection] = useState();
  const [error, setError] = useState();
  const [playersMap, setPlayersMap] = useState({});
  const [name, setName] = useState("");
  const [gameState, setGameState] = useState();
  const [playerId, setPlayerId] = useState();
  const [disableSellDrop, setDisableSellDrop] = useState(true);
  const [disableBoardDrop, setDisableBoardDrop] = useState(true);
  const [disableShopDrop, setDisableShopDrop] = useState(true);
  const [disableHandDrop, setDisableHandDrop] = useState(true);
  const [disableHeroDrop, setDisableHeroDrop] = useState(true);
  const [disableBoardShift, setDisableBoardShift] = useState(false);
  const [disableShopShift, setDisableShopShift] = useState(false);
  const [hasEndedTurn, setHasEndedTurn] = useState(false);
  const [place, setPlace] = useState("");
  const [cardBeingPlayed, setCardBeingPlayed] = useState(null);
  const [reconnecting, setReconnecting] = useState(false);
  const [combatActions, setCombatActions] = useState([]);

  const player = playersMap[playerId];
  const opponent = playersMap[player?.opponentId];

  console.log("playersMap", playersMap);
  console.log("playersId", playerId);

  function removeCardFromHand(cardId) {
    const clonedPlayersMap = cloneDeep(playersMap);

    clonedPlayersMap[playerId].hand = clonedPlayersMap[playerId].hand.filter(
      (card) => card.id !== cardId,
    );

    setPlayersMap(clonedPlayersMap);
  }

  function onDragStart(result) {
    if (result.source.droppableId === "droppable-shop") {
      setDisableHandDrop(false);
      setDisableShopDrop(false);
    }

    if (result.source.droppableId === "droppable-hand") {
      const card = player.hand[result.source.index];
      console.log("card", card.targetOptions);
      setDisableBoardDrop(false);
      setCardBeingPlayed(card);

      // Can't play more than 7 minions
      if (player.board.length >= 7 && card.cardType === 0) {
        setDisableBoardDrop(true);
      }

      // Spell without target
      if (card.cardType === 1 && card.targetOptions === "none") {
        setDisableBoardDrop(true);
        setDisableHeroDrop(false);
      }

      // Spell with board target
      if (card.cardType === 1 && card.targetOptions === "friendly") {
        setDisableBoardShift(true);
      }

      // Spell with board or shop target
      if (card.cardType === 1 && card.targetOptions === "any") {
        setDisableBoardShift(true);
        setDisableShopDrop(false);
        setDisableShopShift(true);
      }
    }

    if (result.source.droppableId === "droppable-board") {
      setDisableBoardDrop(false);
      setDisableSellDrop(false);
    }
  }

  function onDragEnd(result) {
    console.log("Drag End Result", result);
    setCardBeingPlayed(null);
    setDisableSellDrop(true);
    setDisableBoardDrop(true);
    setDisableShopDrop(true);
    setDisableHandDrop(true);
    setDisableHeroDrop(true);
    setDisableBoardShift(false);
    setDisableShopShift(false);

    const cardId = result.draggableId;

    // Play spell without target
    if (
      cardBeingPlayed?.cardType === 1 &&
      cardBeingPlayed?.targetOptions === "none" &&
      result.destination?.droppableId === "droppable-hero"
    ) {
      removeCardFromHand(cardId);

      connection.invoke("MoveCard", result.draggableId, 2, null, null);
      return;
    }

    // Play card with target
    if (result?.combine?.draggableId) {
      removeCardFromHand(cardId);

      connection.invoke(
        "MoveCard",
        result.draggableId,
        2,
        null,
        result.combine.draggableId,
      );
      return;
    }

    // Check if the drag was canceled
    if (!result.destination) return;

    // Swap shop
    if (
      result.source.droppableId === "droppable-shop" &&
      result.destination.droppableId === "droppable-shop"
    ) {
      const clonedPlayersMap = cloneDeep(playersMap);

      const minion = clonedPlayersMap[playerId].shop[result.source.index];

      clonedPlayersMap[playerId].shop.splice(result.source.index, 1);

      clonedPlayersMap[playerId].shop.splice(
        result.destination.index,
        0,
        minion,
      );

      setPlayersMap(clonedPlayersMap);

      connection.invoke(
        "MoveCard",
        result.draggableId,
        4,
        result.destination.index,
        null,
      );
      return;
    }

    // Swap board
    if (
      result.source.droppableId === "droppable-board" &&
      result.destination.droppableId === "droppable-board"
    ) {
      const clonedPlayersMap = cloneDeep(playersMap);

      const minion = clonedPlayersMap[playerId].board[result.source.index];

      clonedPlayersMap[playerId].board.splice(result.source.index, 1);

      clonedPlayersMap[playerId].board.splice(
        result.destination.index,
        0,
        minion,
      );

      setPlayersMap(clonedPlayersMap);

      connection.invoke(
        "MoveCard",
        result.draggableId,
        3,
        result.destination.index,
        null,
      );
      return;
    }

    // Sell
    if (
      result.source.droppableId === "droppable-board" &&
      result.destination.droppableId === "droppable-sell"
    ) {
      const clonedPlayersMap = cloneDeep(playersMap);

      clonedPlayersMap[playerId].board = clonedPlayersMap[
        playerId
      ].board.filter((card) => card.id !== cardId);

      setPlayersMap(clonedPlayersMap);

      connection.invoke("MoveCard", result.draggableId, 1, null, null);
      return;
    }

    // Play
    if (
      result.source.droppableId === "droppable-hand" &&
      result.destination.droppableId === "droppable-board"
    ) {
      const clonedPlayersMap = cloneDeep(playersMap);

      const card = clonedPlayersMap[playerId].hand.find(
        (card) => card.id === cardId,
      );

      clonedPlayersMap[playerId].hand = clonedPlayersMap[playerId].hand.filter(
        (card) => card.id !== cardId,
      );

      clonedPlayersMap[playerId].board.splice(
        result.destination.index,
        0,
        card,
      );

      setPlayersMap(clonedPlayersMap);

      connection.invoke(
        "MoveCard",
        result.draggableId,
        2,
        result.destination.index,
        null,
      );
      return;
    }

    // Buy
    if (
      result.source.droppableId === "droppable-shop" &&
      result.destination.droppableId === "droppable-hand"
    ) {
      const clonedPlayersMap = cloneDeep(playersMap);

      const card = clonedPlayersMap[playerId].shop.find(
        (card) => card.id === cardId,
      );

      clonedPlayersMap[playerId].shop = clonedPlayersMap[playerId].shop.filter(
        (card) => card.id !== cardId,
      );

      clonedPlayersMap[playerId].hand.push(card);

      setPlayersMap(clonedPlayersMap);

      connection.invoke("MoveCard", result.draggableId, 0, null, null);
      return;
    }
  }

  useEffect(() => {
    const url =
      import.meta.env.MODE === "development"
        ? "http://localhost:5043/messageHub"
        : "/messageHub";

    const connection = new signalR.HubConnectionBuilder()
      // .configureLogging(signalR.LogLevel.Debug) // add this for diagnostic clues
      .withUrl(url, {
        skipNegotiation: true, // skipNegotiation as we specify WebSockets
        transport: signalR.HttpTransportType.WebSockets, // force WebSocket transport
      })
      .withAutomaticReconnect()
      .build();

    connection.start().catch((error) => {
      setError(error);
      setGameStatus("error");
    });

    connection.onreconnecting((error) => {
      setReconnecting(true);
      console.log("RECONNECTING", error);
    });

    connection.onclose((error) => {
      console.log("CLOSED", error);
      // setError(error);
    });

    setConnection(connection);

    console.log("hi");

    return () => {
      if (connection?.stop) {
        connection.stop();
      }
    };
  }, []);

  useEffect(() => {
    if (!connection) {
      return;
    }

    connection.onreconnected(() => {
      setReconnecting(false);
      console.log("RECONNECTED");
      if (playerId) {
        connection.invoke("OnReconnected", playerId);
      }
    });
  }, [connection, playerId]);

  useEffect(() => {
    if (!connection) {
      return;
    }

    connection.on("LobbyUpdated", (lobby, playerId) => {
      console.log("LobbyUdated", lobby, playerId);
      if (playerId) {
        setPlayerId(playerId);
      }
      if (gameStatus === "name" || gameStatus === "lobby") {
        setGameStatus("lobby");
      } else {
        setGameStatus("shop");
      }
      setPlayersMap(lobby.players);
    });

    connection.on("GameError", (error) => {
      setError(error);
      setGameStatus("error");
    });

    connection.on("StartGameConfirmed", (lobby) => {
      console.log("StartGameConfirmed", lobby);
      setPlayersMap(lobby.players);
      setGameState(lobby.gameState);
      setGameStatus("shop");
    });

    connection.on("PlayerUpdated", (newPlayer) => {
      console.log("PlayerUpdated", newPlayer);
      setPlayersMap((prev) => ({ ...prev, [newPlayer.id]: newPlayer }));
    });

    connection.on(
      "CombatStarted",
      (combatActions, opponentBoard, playerBoard) => {
        setCombatActions(combatActions);
        setGameStatus("battle");
        setPlayersMap((prev) => {
          const player = prev[playerId]
          const opponent = prev[player.opponentId]
          return {
            ...prev,
            [player.opponentId]: { ...opponent, board: opponentBoard },
            [player.id]: { ...player, board: playerBoard },
          };
        });
        setHasEndedTurn(false);
      },
    );

    connection.on("ReconnectSuccess", (lobby, playerId) => {
      setPlayersMap(lobby.players);
      setPlayerId(playerId);
    });

    return () => {
      connection.off("LobbyUpdated");
      connection.off("GameError");
      connection.off("StartGameConfirmed");
      connection.off("PlayerUpdated");
      connection.off("CombatStarted");
      connection.off("ReconnectSuccess");
    };
  }, [connection, playerId, gameStatus]);

  function endTurn() {
    connection.invoke("EndTurn");
    setHasEndedTurn(true);
  }

  if (gameStatus === "error") {
    return <Error error={error} />;
  }

  if (gameStatus === "lobby") {
    return <Lobby playersMap={playersMap} connection={connection} />;
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

  if (gameStatus === "gameover") {
    return <GameOverPage place={place} />;
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

      {reconnecting && (
        <div className="absolute top-0 right-0 left-0 z-10 flex items-center justify-center bg-red-900 py-2 text-xs text-white">
          Reconnecting...
        </div>
      )}

      <div className="relative h-screen w-screen overflow-hidden bg-[url(/sky.jpg)] bg-cover">
        {gameStatus === "shop" && (
          <ShopBoard
            connection={connection}
            player={player}
            disableSellDrop={disableSellDrop}
            disableBoardDrop={disableBoardDrop}
            disableShopDrop={disableShopDrop}
            disableHandDrop={disableHandDrop}
            cardBeingPlayed={cardBeingPlayed}
            disableBoardShift={disableBoardShift}
            disableShopShift={disableShopShift}
            disableHeroDrop={disableHeroDrop}
          />
        )}
        {gameStatus === "battle" && (
          <BattleBoard
            connection={connection}
            initialPlayer={player}
            initialOpponent={opponent}
            setGameStatus={setGameStatus}
            setPlace={setPlace}
            combatActions={combatActions}
          />
        )}
        {gameStatus === "shop" && (
          <Button
            className="absolute top-1/2 right-0"
            onClick={endTurn}
            disabled={hasEndedTurn}
          >
            End Turn
          </Button>
        )}
        <Gold gold={player?.gold} maxGold={player?.baseGold} />
        <Opponents playersMap={playersMap} opponentId={player?.opponentId} />
      </div>
    </DragDropContext>
  );
}

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
  const [players, setPlayers] = useState([]);
  const [name, setName] = useState("");
  const [gameState, setGameState] = useState();
  const [playerId, setPlayerId] = useState();
  const [disableSellDrop, setDisableSellDrop] = useState(false);
  const [disableBoardDrop, setDisableBoardDrop] = useState(false);
  const [disableShopDrop, setDisableShopDrop] = useState(false);
  const [disableHandDrop, setDisableHandDrop] = useState(false);
  const [disableBoardShift, setDisableBoardShift] = useState(false);
  const [disableShopShift, setDisableShopShift] = useState(false);
  const [hasEndedTurn, setHasEndedTurn] = useState(false);
  const [winner, setWinner] = useState("");
  const [cardBeingPlayed, setCardBeingPlayed] = useState(null);
  const [reconnecting, setReconnecting] = useState(false);

  const player = players.find((player) => player.id === playerId);
  const opponent = players.find((x) => x.id === player?.currentOpponentId);
  const combatOpponent = players.find((x) => x.id === player?.combatOpponentId);

  console.log("players", players);

  function removeCardFromHand(cardId) {
    const clonedPlayers = cloneDeep(players);

    const playerIndex = clonedPlayers.findIndex(
      (player) => player.id === playerId,
    );

    clonedPlayers[playerIndex].board = clonedPlayers[playerIndex].board.filter(
      (card) => card.id !== cardId,
    );

    setPlayers(clonedPlayers);
  }

  function onDragStart(result) {
    if (result.source.droppableId === "droppable-shop") {
      setDisableSellDrop(true);
      setDisableBoardDrop(true);
    }

    if (result.source.droppableId === "droppable-hand") {
      const card = player.hand[result.source.index];
      setDisableSellDrop(true);
      setCardBeingPlayed(card);

      if (card.cardType === 0 || card.targetOptions !== "any") {
        setDisableShopDrop(true);
      }

      // Can't play more than 7 minions
      if (player.board.length >= 7 && card.cardType === 0) {
        setDisableBoardDrop(true);
      }

      // Spell without target
      if (card.cardType === 1 && card.targetOptions === "none") {
        setDisableBoardDrop(true);
      }

      // Spell with target
      if (card.cardType === 1 && card.targetOptions !== "none") {
        setDisableBoardShift(true);
        setDisableShopShift(true);
      }
    }

    if (result.source.droppableId === "droppable-board") {
      setDisableShopDrop(true);
      setDisableHandDrop(true);
    }
  }

  function onDragEnd(result) {
    console.log("result", result);
    setCardBeingPlayed(null);
    setDisableSellDrop(false);
    setDisableBoardDrop(false);
    setDisableShopDrop(false);
    setDisableHandDrop(false);
    setDisableBoardShift(false);
    setDisableShopShift(false);

    const cardId = result.draggableId;

    // Play spell without target
    if (
      cardBeingPlayed?.cardType === 1 &&
      cardBeingPlayed?.targetOptions === "none" &&
      !result.destination
    ) {
      console.log("play spell");
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
      const clonedPlayers = cloneDeep(players);

      const playerIndex = clonedPlayers.findIndex(
        (player) => player.id === playerId,
      );

      const minion = clonedPlayers[playerIndex].shop[result.source.index];

      clonedPlayers[playerIndex].shop.splice(result.source.index, 1);

      clonedPlayers[playerIndex].shop.splice(
        result.destination.index,
        0,
        minion,
      );

      setPlayers(clonedPlayers);

      // connection.invoke(
      //   "MoveCard",
      //   result.draggableId,
      //   3,
      //   result.destination.index,
      //   null,
      // );
      return;
    }

    // Swap board
    if (
      result.source.droppableId === "droppable-board" &&
      result.destination.droppableId === "droppable-board"
    ) {
      const clonedPlayers = cloneDeep(players);

      const playerIndex = clonedPlayers.findIndex(
        (player) => player.id === playerId,
      );

      const minion = clonedPlayers[playerIndex].board[result.source.index];

      clonedPlayers[playerIndex].board.splice(result.source.index, 1);

      clonedPlayers[playerIndex].board.splice(
        result.destination.index,
        0,
        minion,
      );

      setPlayers(clonedPlayers);

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
      const clonedPlayers = cloneDeep(players);

      const playerIndex = clonedPlayers.findIndex(
        (player) => player.id === playerId,
      );

      clonedPlayers[playerIndex].board = clonedPlayers[
        playerIndex
      ].board.filter((card) => card.id !== cardId);

      setPlayers(clonedPlayers);

      connection.invoke("MoveCard", result.draggableId, 1, null, null);
      return;
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
      return;
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

    console.log("connection", connection);

    setConnection(connection);

    return () => {
      connection.close();
    };
  }, []);

  useEffect(() => {
    if (!connection || !playerId) {
      return;
    }

    connection.onreconnected(() => {
      setReconnecting(false);
      console.log("RECONNECTED");
      connection.invoke("OnReconnected", playerId);
    });
  }, [connection, playerId]);

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

    connection.on("CombatComplete", (lobby) => {
      setPlayers(lobby.players);
      setGameState(lobby.gameState);
      setGameStatus("battle");
      setHasEndedTurn(false);
    });

    connection.on("ReconnectSuccess", (lobby, playerId) => {
      setPlayers(lobby.players);
      setPlayerId(playerId);
    });

    return () => {
      connection.off("LobbyUpdated");
      connection.off("GameError");
      connection.off("StartGameConfirmed");
      connection.off("PlayerUpdated");
      connection.off("CombatComplete");
      connection.off("ReconnectSuccess");
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

  if (gameStatus === "gameover") {
    return <GameOverPage winner={winner} />;
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
          />
        )}
        {gameStatus === "battle" && (
          <BattleBoard
            initialPlayer={player}
            initialOpponent={combatOpponent}
            setGameStatus={setGameStatus}
            setWinner={setWinner}
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
        <Opponents
          players={players}
          opponentId={
            gameStatus === "battle"
              ? player?.combatOpponentId
              : player?.currentOpponentId
          }
        />
      </div>
    </DragDropContext>
  );
}

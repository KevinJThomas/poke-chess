import { delay, getElementPosition } from "../util";
import Hero from "./Hero";
import OpponentPokemon from "./OpponentPokemon";
import PlayerPokemon from "./PlayerPokemon";
import Row from "./Row";
import useAsyncEffect from "use-async-effect";
import { useState } from "react";

// const combatActions = [
//   {
//     playerMinionId: 1,
//     opponentMinionId: 3,
//     playerOnHitValues: { damage: 2, health: 1 },
//     opponentOnHitValues: { damage: 3, health: 0 },
//     playerIsAttacking: true,
//     type: "minion",
//   },
//   {
//     playerMinionId: 1,
//     opponentMinionId: 4,
//     playerOnHitValues: { damage: 5, health: -4 },
//     opponentOnHitValues: { damage: 3, health: 2 },
//     playerIsAttacking: false,
//     type: "minion",
//   },
//   {
//     playerIsAttacking: true,
//     onHitValues: { damage: 3, armor: 2 },
//     type: "hero",
//   },
// ];

export default function BattleBoard({
  initialPlayer,
  initialOpponent,
  setGameStatus,
  setWinner,
}) {
  const [player, setPlayer] = useState(initialPlayer);
  const [opponent, setOpponent] = useState(initialOpponent);
  // const [player, setPlayer] = useState({
  //   name: "p1",
  //   health: 30,
  //   armor: 5,
  //   id: "p1",
  //   board: [
  //     {
  //       id: 1,
  //       num: "002",
  //       attack: 3,
  //       health: 3,
  //       cardType: 0,
  //     },
  //     {
  //       id: 2,
  //       num: "003",
  //       attack: 3,
  //       health: 4,
  //       cardType: 0,
  //     },
  //   ],
  //   hand: [],
  // });
  // const [opponent, setOpponent] = useState({
  //   name: "opponent",
  //   id: "opponent",
  //   health: 30,
  //   armor: 5,
  //   board: [
  //     {
  //       id: 3,
  //       num: "004",
  //       attack: 2,
  //       health: 3,
  //       cardType: 0,
  //     },
  //     {
  //       id: 4,
  //       num: "005",
  //       attack: 5,
  //       health: 5,
  //       cardType: 0,
  //     },
  //   ],
  //   hand: [],
  // });

  function updatePlayerMinion(playerMinionIndex, values) {
    setPlayer((prev) => ({
      ...prev,
      board: prev.board.map((minion, i) => {
        if (playerMinionIndex === i) {
          return {
            ...minion,
            ...values,
          };
        }

        return minion;
      }),
    }));
  }

  function updateOpponentMinion(opponentMinionIndex, values) {
    setOpponent((prev) => ({
      ...prev,
      board: prev.board.map((minion, i) => {
        if (opponentMinionIndex === i) {
          return {
            ...minion,
            ...values,
          };
        }

        return minion;
      }),
    }));
  }

  async function attackHero(action) {
    const [playerHeroTop, playerHeroLeft] = getElementPosition("player-hero");
    const [opponentHeroTop, opponentHeroLeft] =
      getElementPosition("opponent-hero");

    const updateHeroFunc = action.playerIsAttacking ? setPlayer : setOpponent;
    const updateLoserFunc = action.playerIsAttacking ? setOpponent : setPlayer;

    const startCoords = action.playerIsAttacking
      ? [playerHeroTop, playerHeroLeft]
      : [opponentHeroTop, opponentHeroLeft];

    const endCoords = action.playerIsAttacking
      ? [opponentHeroTop + 30, opponentHeroLeft]
      : [playerHeroTop - 30, playerHeroLeft];

    updateHeroFunc((prev) => ({
      ...prev,
      style: {
        position: "fixed",
        top: startCoords[0],
        left: startCoords[1],
        zIndex: 10,
      },
    }));

    await delay(1000);

    updateHeroFunc((prev) => ({
      ...prev,
      style: {
        position: "fixed",
        top: endCoords[0],
        left: endCoords[1],
        zIndex: 10,
      },
    }));

    await delay(400);

    updateLoserFunc((prev) => ({ ...prev, ...action.onHitValues }));

    updateHeroFunc((prev) => ({
      ...prev,
      style: {
        position: "fixed",
        top: startCoords[0],
        left: startCoords[1],
        zIndex: 10,
      },
    }));
  }
  async function attackMinion(action, index) {
    try {
      const [playerMinionTop, playerMinionLeft] = getElementPosition(
        action.playerMinionId,
      );
      const [opponentMinionTop, opponentMinionLeft] = getElementPosition(
        action.opponentMinionId,
      );

      const playerMinionIndex = player.board.findIndex(
        (minion) => minion.id === action.playerMinionId,
      );
      const opponentMinionIndex = opponent.board.findIndex(
        (minion) => minion.id === action.opponentMinionId,
      );

      const startCoords = action.playerIsAttacking
        ? [playerMinionTop, playerMinionLeft]
        : [opponentMinionTop, opponentMinionLeft];

      const endCoords = action.playerIsAttacking
        ? [opponentMinionTop + 30, opponentMinionLeft]
        : [playerMinionTop - 30, playerMinionLeft];

      const attackingMinionIndex = action.playerIsAttacking
        ? playerMinionIndex
        : opponentMinionIndex;

      const updateMinionFunc = action.playerIsAttacking
        ? updatePlayerMinion
        : updateOpponentMinion;

      updateMinionFunc(attackingMinionIndex, {
        style: {
          position: "fixed",
          top: startCoords[0],
          left: startCoords[1],
          zIndex: 10,
        },
      });

      await delay(1000);

      updateMinionFunc(attackingMinionIndex, {
        style: {
          position: "fixed",
          top: endCoords[0],
          left: endCoords[1],
          zIndex: 10,
        },
      });

      await delay(200);

      updatePlayerMinion(playerMinionIndex, action.playerOnHitValues);
      updateOpponentMinion(opponentMinionIndex, action.opponentOnHitValues);

      updateMinionFunc(attackingMinionIndex, {
        style: {
          position: "fixed",
          top: startCoords[0],
          left: startCoords[1],
          zIndex: 10,
        },
      });

      await delay(300);

      updateMinionFunc(attackingMinionIndex, {
        style: {
          position: "relative",
          top: "",
          left: "",
          zIndex: "auto",
        },
      });

      await delay(1000);
    } catch (error) {
      console.error(error);
      console.error("Action index that failed:", index);
      console.error("All combat actions", player.combatActions);
    }
  }

  useAsyncEffect(async () => {
    await delay(1000);

    console.log(player.combatActions);

    for (const [index, action] of player.combatActions.entries()) {
      if (action.type === "minion") {
        await attackMinion(action, index);
      }

      if (action.type === "hero") {
        await attackHero(action);
      }

      if (action.type === "gameover") {
        setGameStatus("gameover");
        setWinner(action.winnerName);
        return;
      }

      await delay(250);
    }
    await delay(3000);

    setGameStatus("shop");
  }, []);

  return (
    <>
      <div className="flex h-screen flex-col items-center justify-center">
        <Row>
          <Hero
            name={opponent.name}
            health={opponent.health}
            armor={opponent.armor}
            style={opponent.style}
            damage={opponent.damage}
            id="opponent-hero"
          />
        </Row>
        <Row>
          <OpponentPokemon opponent={opponent} />
        </Row>
        <Row>
          <PlayerPokemon player={player} />
        </Row>
        <Row>
          <Hero
            name={player.name}
            health={player.health}
            armor={player.armor}
            style={player.style}
            damage={player.damage}
            id="player-hero"
          />
        </Row>
        <Row></Row>
      </div>
    </>
  );
}

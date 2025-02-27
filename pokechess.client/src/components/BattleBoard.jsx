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
//     playerOnHitValues: [{ id: 1, damage: 2, health: 1 }],
//     opponentOnHitValues: [{ id: 3, damage: 3, health: 0 }],
//     playerIsAttacking: true,
//     type: "minion",
//   },
//   {
//     playerMinionId: 1,
//     opponentMinionId: 4,
//     playerOnHitValues: [{ id: 1, damage: 5, health: -4 }],
//     opponentOnHitValues: [{ id: 4, damage: 3, health: 2 }],
//     playerIsAttacking: false,
//     type: "minion",
//   },
//   {
//     playerIsAttacking: true,
//     heroOnHitValues: { damage: 3, armor: 2 },
//     type: "hero",
//   },
// ];

export default function BattleBoard({
  initialPlayer,
  initialOpponent,
  setGameStatus,
  setPlace,
  connection,
  combatActions,
}) {
  const [player, setPlayer] = useState(initialPlayer);
  const [opponent, setOpponent] = useState(initialOpponent);

  console.log("combat actions", combatActions);
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

  function updatePlayerMinions(values) {
    setPlayer((prev) => ({
      ...prev,
      board: prev.board.map((minion) => {
        const value = values.find((value) => value.id === minion.id);
        if (value?.id === minion.id) {
          return {
            ...minion,
            ...value,
          };
        }

        return minion;
      }),
    }));
  }

  function updateOpponentMinions(values) {
    setOpponent((prev) => ({
      ...prev,
      board: prev.board.map((minion) => {
        const value = values.find((value) => value.id === minion.id);
        if (value?.id === minion.id) {
          return {
            ...minion,
            ...value,
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

    updateLoserFunc((prev) => ({ ...prev, ...action.heroOnHitValues }));

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

  async function startOfCombat(action) {
    await delay(1000);

    updatePlayerMinions(action.playerOnHitValues);
    updateOpponentMinions(action.opponentOnHitValues);
  }

  async function attackMinion(action, index) {
    try {
      const [playerMinionTop, playerMinionLeft] = getElementPosition(
        action.playerMinionId,
      );
      const [opponentMinionTop, opponentMinionLeft] = getElementPosition(
        action.opponentMinionId,
      );

      const startCoords = action.playerIsAttacking
        ? [playerMinionTop, playerMinionLeft]
        : [opponentMinionTop, opponentMinionLeft];

      const endCoords = action.playerIsAttacking
        ? [opponentMinionTop + 30, opponentMinionLeft]
        : [playerMinionTop - 30, playerMinionLeft];

      const attackingMinionId = action.playerIsAttacking
        ? action.playerMinionId
        : action.opponentMinionId;

      const updateMinionFunc = action.playerIsAttacking
        ? updatePlayerMinions
        : updateOpponentMinions;

      updateMinionFunc([
        {
          id: attackingMinionId,
          style: {
            position: "fixed",
            top: startCoords[0],
            left: startCoords[1],
            zIndex: 10,
          },
        },
      ]);

      await delay(1000);

      updateMinionFunc([
        {
          id: attackingMinionId,
          style: {
            position: "fixed",
            top: endCoords[0],
            left: endCoords[1],
            zIndex: 10,
          },
        },
      ]);

      await delay(200);

      updatePlayerMinions(action.playerOnHitValues);
      updateOpponentMinions(action.opponentOnHitValues);

      updateMinionFunc([
        {
          id: attackingMinionId,
          style: {
            position: "fixed",
            top: startCoords[0],
            left: startCoords[1],
            zIndex: 10,
          },
        },
      ]);

      await delay(300);

      updateMinionFunc([
        {
          id: attackingMinionId,
          style: {
            position: "relative",
            top: "",
            left: "",
            zIndex: "auto",
          },
        },
      ]);

      await delay(1000);
    } catch (error) {
      console.error(error);
      console.error("Action index that failed:", index);
      console.error("All combat actions", combatActions);
    }
  }

  useAsyncEffect(async () => {
    await delay(1000);

    for (const [index, action] of combatActions.entries()) {
      if (action.type === "startofcombat") {
        await startOfCombat(action);
      }

      if (action.type === "minion") {
        await attackMinion(action, index);
      }

      if (action.type === "hero") {
        await attackHero(action);
      }

      if (action.type === "gameover") {
        setGameStatus("gameover");
        setPlace(action.placement);
        return;
      }

      await delay(250);
    }
    await delay(3000);

    connection.invoke("CombatComplete");
    console.log("CombatComplete");
  }, []);

  return (
    <>
      <div className="flex h-screen flex-col items-center justify-center">
        <Row>
          <Hero {...opponent} type="opponent" id="opponent-hero" />
        </Row>
        <Row>
          <OpponentPokemon opponent={opponent} />
        </Row>
        <Row>
          <PlayerPokemon player={player} />
        </Row>
        <Row>
          <Hero {...player} type="player-battle" id="player-hero" />
        </Row>
        <Row></Row>
      </div>
    </>
  );
}

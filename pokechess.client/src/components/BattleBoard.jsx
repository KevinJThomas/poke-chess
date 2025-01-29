import HandRow from "./HandRow";
import Hero from "./Hero";
import OpponentPokemon from "./OpponentPokemon";
import PlayerPokemon from "./PlayerPokemon";
import Row from "./Row";

export default function BattleBoard({ player, opponent, gameState }) {
  console.log("opponent", opponent);
  console.log("player", player);
  return (
    <div className="flex h-screen flex-col items-center justify-center">
      <Row>
        <Hero
          name={opponent.name}
          health={opponent.health}
          armor={opponent.armor}
        />
      </Row>
      <Row>
        <OpponentPokemon opponent={opponent} />
      </Row>
      <Row>
        <PlayerPokemon
          isDragDisabled={true}
          isDropDisabled={true}
          player={player}
        />
      </Row>
      <Row>
        <Hero name={player.name} health={player.health} armor={player.armor} />
      </Row>
      <Row>
        <HandRow player={player} isDragDisabled={true} isDropDisabled={true} />
      </Row>
    </div>
  );
}

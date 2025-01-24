import HandRow from "./HandRow";
import Hero from "./Hero";
import PlayerPokemon from "./PlayerPokemon";
import Row from "./Row";
import TavernRow from "./TavernRow";

export default function BattleBoard() {
  return (
    <div className="flex h-screen flex-col items-center justify-center">
      <Row>
        <Hero name="Brock" health="30" armor="10" />
      </Row>
      <Row>
        <TavernRow isDragDisabled={true} />
      </Row>
      <Row>
        <PlayerPokemon />
      </Row>
      <Row>
        <Hero name="Ash" health={30} armor={10} />
      </Row>
      <Row>
        <HandRow isDragDisabled={true} />
      </Row>
    </div>
  );
}

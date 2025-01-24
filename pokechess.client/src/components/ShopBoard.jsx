import Button from "./Button";
import HandRow from "./HandRow";
import Hero from "./Hero";
import PlayerPokemon from "./PlayerPokemon";
import Row from "./Row";
import Shop from "./Shop";
import TavernRow from "./TavernRow";

export default function ShopBoard() {
  const tavern = [
    { id: "1", name: "Pikachu", attack: 10, health: 100, tier: 1 },
    { id: "2", name: "Charmander", attack: 10, health: 100, tier: 1 },
    { id: "3", name: "Bulbasaur", attack: 10, health: 100, tier: 1 },
  ];

  // const hand = [
  //   { id: "1", name: "Pikachu", attack: 10, health: 100, tier: 1 },
  //   { id: "2", name: "Pikachu", attack: 10, health: 100, tier: 1 },
  //   { id: "3", name: "Pikachu", attack: 10, health: 100, tier: 1 },
  // ];

  return (
    <div className="flex h-screen flex-col items-center justify-center">
      <Row>
        <div className="w-16"></div>
        <Button className="">Upgrade (6)</Button>
        <Shop />
        <Button className="">Refresh</Button>
        <Button className="">Freeze</Button>
      </Row>
      <TavernRow tavern={tavern} isDragDisabled={false} />
      <Row>
        <PlayerPokemon />
      </Row>
      <Row>
        <Hero name="Ash" health={30} armor={10} />
      </Row>
      <HandRow isDragDisabled={false} />
    </div>
  );
}

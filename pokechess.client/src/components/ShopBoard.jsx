import Button from "./Button";
import HandRow from "./HandRow";
import Hero from "./Hero";
import PlayerPokemon from "./PlayerPokemon";
import Row from "./Row";
import Shop from "./Shop";
import TavernRow from "./TavernRow";

export default function ShopBoard({ players, gameState, connection, player }) {
  console.log("players", players);
  console.log("gameState", gameState);

  function upgrade() {
    connection.invoke("Upgrade");
  }

  function refresh() {
    console.log("get new shop");
    connection.invoke("GetNewShop");
  }

  function freeze() {
    connection.invoke("Freeze");
  }
  // const hand = [
  //   { id: "1", name: "Pikachu", attack: 10, health: 100, tier: 1 },
  //   { id: "2", name: "Pikachu", attack: 10, health: 100, tier: 1 },
  //   { id: "3", name: "Pikachu", attack: 10, health: 100, tier: 1 },
  // ];

  return (
    <div className="flex h-screen flex-col items-center justify-center">
      <Row>
        <div className="w-16"></div>
        <Button onClick={upgrade}>Upgrade ({player.upgradeCost})</Button>
        <Shop player={player} />
        <Button onClick={refresh} disabled={player.refreshCost > player.gold}>
          Refresh ({player.refreshCost})
        </Button>
        <Button onClick={freeze}>Freeze (0)</Button>
      </Row>
      <TavernRow tavern={player?.shop ?? []} isDragDisabled={false} />
      <Row>
        <PlayerPokemon board={player?.board ?? []} />
      </Row>
      <Row>
        <Hero name={player.name} health={player.health} armor={player.armor} />
      </Row>
      <HandRow hand={player.hand} isDragDisabled={false} />
    </div>
  );
}

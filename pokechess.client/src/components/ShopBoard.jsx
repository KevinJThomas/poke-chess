import Button from "./Button";
import HandRow from "./HandRow";
import Hero from "./Hero";
import PlayerPokemon from "./PlayerPokemon";
import Row from "./Row";
import Shop from "./Shop";
import TavernRow from "./TavernRow";

export default function ShopBoard({
  players,
  gameState,
  connection,
  player,
  disableSellDrop,
  disableBoardDrop,
  disableShopDrop,
  disableHandDrop,
}) {
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
    connection.invoke("FreezeShop");
  }

  return (
    <div className="flex h-screen flex-col items-center justify-center">
      <Row>
        <div className="w-16"></div>
        <Button onClick={upgrade}>Upgrade ({player.upgradeCost})</Button>
        <Shop player={player} isDropDisabled={disableSellDrop} />
        <Button onClick={refresh} disabled={player.refreshCost > player.gold}>
          Refresh ({player.refreshCost})
        </Button>
        <Button onClick={freeze}>Freeze (0)</Button>
      </Row>
      <TavernRow
        player={player}
        isDragDisabled={false}
        isDropDisabled={disableShopDrop}
      />
      <Row>
        <PlayerPokemon
          board={
            player.board.length === 0 ? [{ id: "empty-slot" }] : player.board
          }
          player={player}
          isDropDisabled={disableBoardDrop}
        />
      </Row>
      <Row>
        <Hero name={player.name} health={player.health} armor={player.armor} />
      </Row>
      <HandRow
        hand={player.hand.length === 0 ? [{ id: "empty-slot" }] : player.hand}
        isDragDisabled={false}
        isDropDisabled={disableHandDrop}
      />
    </div>
  );
}

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
  function upgrade() {
    connection.invoke("UpgradeTavern");
  }

  function refresh() {
    connection.invoke("GetNewShop");
  }

  function freeze() {
    connection.invoke("FreezeShop");
  }

  return (
    <div className="flex h-screen flex-col items-center justify-center">
      <Row>
        <div className="w-16"></div>
        <Button onClick={upgrade} disabled={player.upgradeCost > player.gold}>
          Upgrade ({player.upgradeCost})
        </Button>
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
      <PlayerPokemon player={player} isDropDisabled={disableBoardDrop} />
      <Row>
        <Hero name={player.name} health={player.health} armor={player.armor} />
      </Row>
      <HandRow
        player={player}
        isDragDisabled={false}
        isDropDisabled={disableHandDrop}
      />
    </div>
  );
}

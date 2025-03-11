import Button from "./Button";
import DiscoverModal from "./DiscoverModal";
import HandRow from "./HandRow";
import Hero from "./Hero";
import HeroPower from "./HeroPower";
import PlayerBoard from "./PlayerBoard";
import Row from "./Row";
import Shop from "./Shop";
import TavernRow from "./TavernRow";

export default function ShopBoard({
  connection,
  player,
  disableSellDrop,
  disableBoardDrop,
  disableShopDrop,
  disableHandDrop,
  disableShopShift,
  cardBeingPlayed,
  disableBoardShift,
  disableHeroDrop,
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
        <Button
          onClick={upgrade}
          disabled={player.upgradeCost > player.gold || player.tier >= 6}
        >
          Upgrade ({player.upgradeCost})
        </Button>
        <Shop tier={player.tier} isDropDisabled={disableSellDrop} />
        <Button onClick={refresh} disabled={player.refreshCost > player.gold}>
          Refresh ({player.refreshCost})
        </Button>
        <Button onClick={freeze}>Freeze (0)</Button>
      </Row>
      <TavernRow
        player={player}
        isDragDisabled={false}
        isDropDisabled={disableShopDrop}
        isCombineEnabled={cardBeingPlayed?.targetOptions === "any"}
        isShiftDisabled={disableShopShift}
      />
      <PlayerBoard
        player={player}
        isDropDisabled={disableBoardDrop}
        isCombineEnabled={
          cardBeingPlayed?.targetOptions === "any" ||
          cardBeingPlayed?.targetOptions === "friendly"
        }
        isShiftDisabled={disableBoardShift}
      />
      <Row>
        <div className="w-20"></div>
        <Hero
          name={player.name}
          health={player.health}
          armor={player.armor}
          hero={player.hero}
          type="player-shop"
          isDropDisabled={disableHeroDrop}
        />
        <div>
          <HeroPower {...player.hero.heroPower} connection={connection} />
        </div>
      </Row>
      <HandRow
        player={player}
        isDragDisabled={false}
        isDropDisabled={disableHandDrop}
      />
      <DiscoverModal discoverOptions={player.discoverOptions} connection={connection} />
    </div>
  );
}

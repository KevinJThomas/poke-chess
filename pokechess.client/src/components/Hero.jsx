import Armor from "./Armor";
import Health from "./Health";
import Tier from "./Tier";

export default function Hero({ health, name, armor, tier }) {
  return (
    <div className="relative flex h-24 w-24 items-center justify-center bg-blue-400">
      <span>{name}</span>
      {!!health && <Health health={health} />}
      {!!armor && <Armor armor={armor} />}
      {!!tier && <Tier tier={tier} />}
    </div>
  );
}

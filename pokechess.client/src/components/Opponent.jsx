import Armor from "./Armor";
import Health from "./Health";
import Tier from "./Tier";

export default function Opponent({ health, armor, tier, name }) {
  return (
    <div className="relative flex h-18 w-18 items-center justify-center border-2 border-red-900 bg-red-300">
      <span className="text-xs">{name}</span>
      {health && <Health health={health} />}
      {armor && <Armor armor={armor} />}
      {tier && <Tier tier={tier} />}
    </div>
  );
}

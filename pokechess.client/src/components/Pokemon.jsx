import Attack from "./Attack";
import Health from "./Health";
import Tier from "./Tier";

export default function Pokemon({ attack, health, name, tier }) {
  return (
    <div className="relative flex h-20 w-20 items-center justify-center bg-blue-400">
      <span className="text-xs">{name}</span>
      <Attack attack={attack} />
      <Health health={health} />
      {tier && <Tier tier={tier} />}
    </div>
  );
}

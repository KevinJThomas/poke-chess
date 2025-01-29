import Armor from "./Armor";
import Health from "./Health";
import Tier from "./Tier";
import clsx from "clsx";

export default function Hero({ health, name, armor, tier, className }) {
  return (
    <div
      className={clsx(
        "relative flex h-24 w-24 items-center justify-center rounded-xl bg-blue-400",
        className,
      )}
    >
      <span>{name}</span>
      {!!health && <Health health={health} />}
      {!!armor && <Armor armor={armor} />}
      {!!tier && <Tier tier={tier} />}
    </div>
  );
}

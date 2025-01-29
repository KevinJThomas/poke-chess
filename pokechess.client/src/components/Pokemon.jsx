import Attack from "./Attack";
import Health from "./Health";
import Tier from "./Tier";
import clsx from "clsx";
import Cost from "./Cost";

export default function Pokemon({
  attack,
  health,
  name,
  tier,
  id,
  cardType,
  cost,
  num,
  showTier = false,
}) {
  const isMinion = cardType === 0;

  return (
    <div
      id={id}
      style={{ backgroundImage: `url(/pokemon/${num}.png)` }}
      className={clsx(
        "relative flex h-20 w-20 items-center justify-center",
        isMinion && `bg-contain bg-center`,
        !isMinion && "rounded-xl bg-blue-400",
      )}
      onClick={() => {
        const element = document.getElementById(id);
        const rect = element.getBoundingClientRect();

        const x = rect.left + window.scrollX;
        const y = rect.top + window.scrollY;

        console.log(x, y);
      }}
    >
      {!isMinion && <span className="text-center text-xs">{name}</span>}
      {isMinion && <Attack attack={attack} />}
      {isMinion && <Health health={health} />}
      {!!tier && showTier && <Tier tier={tier} />}
      {!isMinion && Number.isInteger(cost) && <Cost cost={cost} />}
    </div>
  );
}

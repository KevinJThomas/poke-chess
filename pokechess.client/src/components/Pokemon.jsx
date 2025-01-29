import Attack from "./Attack";
import Health from "./Health";
import Tier from "./Tier";
import { useState } from "react";
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

  const [line, setLine] = useState(null);

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
        const line = new LeaderLine(
          document.getElementById(id),
          document.getElementById("item-0-tavern"),
          {
            draw: { animation: true, duration: 1000 },
            path: "fluid",
            color: "red",
            size: 8,
            hide: true,
          },
        );

        line.show({ draw: { duration: 300 } });

        setTimeout(() => {
          line.remove();
        }, 1000);

        setLine(line);
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

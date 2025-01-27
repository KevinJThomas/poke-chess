import Attack from "./Attack";
import Health from "./Health";
import Tier from "./Tier";
import LeaderLine from "react-leader-line";
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
}) {
  const isMinion = cardType === 0;

  const [line, setLine] = useState(null);
  return (
    <div
      id={id}
      className={clsx(
        id === "empty-slot" && "bg-gray-200",
        "relative flex h-20 w-20 items-center justify-center bg-blue-400",
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
      <span className="text-xs">{name}</span>
      {isMinion && <Attack attack={attack} />}
      {isMinion && <Health health={health} />}
      {!!tier && <Tier tier={tier} />}
      {!isMinion && Number.isInteger(cost) && <Cost cost={cost} />}
    </div>
  );
}

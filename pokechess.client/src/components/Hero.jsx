import Armor from "./Armor";
import Health from "./Health";
import Tier from "./Tier";
import clsx from "clsx";
import useAsyncEffect from "use-async-effect";
import { useState } from "react";
import { delay } from "../util";
import HeroDamage from "./HeroDamage";

export default function Hero({
  health,
  name,
  armor,
  tier,
  className,
  id,
  style,
  damage,
}) {
  const [showDamage, setShowDamage] = useState(false);

  useAsyncEffect(async () => {
    setShowDamage(true);
    await delay(3000);
    setShowDamage(false);
  }, [damage]);

  return (
    <div
      id={id}
      className={clsx(
        "relative flex h-24 w-24 items-center justify-center rounded-xl bg-blue-400 transition-all duration-400 ease-in-out",
        className,
      )}
      style={style}
    >
      <span>{name}</span>
      {!!health && <Health health={health} />}
      {!!armor && <Armor armor={armor} />}
      {!!tier && <Tier tier={tier} />}
      {!!damage && showDamage && <HeroDamage damage={damage} />}
    </div>
  );
}

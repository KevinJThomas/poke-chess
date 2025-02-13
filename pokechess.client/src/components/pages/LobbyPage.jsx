import Avatar from "../Avatar";
import Button from "../ButtonPage";
import Card from "../Card";
import Header from "../Header";
import { useState } from "react";

export default function Lobby({ playersMap, connection }) {
  const [isLoading, setIsLoading] = useState(false);
  function startGame() {
    connection.invoke("StartGame");
    setIsLoading(true);
  }

  return (
    <div className="flex h-screen flex-col items-center justify-center bg-indigo-900">
      <Header>Lobby</Header>
      <Card>
        <ul role="list" className="divide-y divide-gray-200">
          {Object.keys(playersMap).map((playerId) => (
            <li key={playerId} className="flex gap-x-4 py-1">
              <div className="flex w-full min-w-52 items-center">
                <Avatar name={playersMap[playerId].name} />
                <p className="text-sm/6 text-gray-900">
                  {playersMap[playerId].name}
                </p>
              </div>
            </li>
          ))}
        </ul>
        <Button
          fullWidth
          // disabled={players.length <= 1}
          onClick={startGame}
          loading={isLoading}
        >
          Start Game
        </Button>
      </Card>
    </div>
  );
}

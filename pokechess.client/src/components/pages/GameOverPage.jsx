import Header from "../Header";
import Card from "../Card";
import Button from "../ButtonPage";

export default function GameOverPage({ place }) {
  function playAgain() {
    window.location.href = "/";
  }

  const placeMap = {
    1: "1st!",
    2: "2nd",
    3: "3rd",
    4: "4th",
    5: "5th",
    6: "6th",
    7: "7th",
    8: "8th",
  };

  return (
    <div className="flex h-screen flex-col items-center justify-center bg-indigo-900">
      <Header>{placeMap[place]}</Header>
      <Card>
        {/* <h3 className="text-base font-semibold text-gray-900">Play again?</h3> */}
        {/* <form className="mt-5 sm:flex sm:items-center">
          <div className="w-full sm:max-w-xs"></div> */}
        <Button fullWidth onClick={playAgain}>
          Play again?
        </Button>
        {/* </form> */}
      </Card>
    </div>
  );
}

class Login extends React.Component {
    constructor(props) {
        super(props);
        this.state = {
            username: "",
            password: ""
        }
        this.onLogin = this.onLogin.bind(this);
    }
    onLogin() {
        let self = this;
        let url = "/api/login";
        let data = {
            username: this.state.username,
            password: this.state.password
        }

        fetch_data(url, data)
            .then(data => data.json())
            .then(data => {
                if (!data.logged)
                    self.shakeWindow();
                else {
                    self.props.onLogin(data);
                }

            })
            .catch((err) => {
                console.error("Errore durante l'elaborazione della richiesta", err);
                self.shakeWindow();
            });


    }
    shakeWindow() {
        let logWin = $("#loginWindow");
        let values = [50, 100];
        logWin.animate(
            {
                left: "+=" + values[0] + "px"
            }, 75, function () {
                logWin.animate(
                    {
                        left: "-=" + values[1] + "px"
                    }, 75, function () {
                        logWin.animate(
                            {
                                left: "+=" + values[1] + "px"
                            }, 75, function () {
                                logWin.animate(
                                    {
                                        left: "-=" + values[1] + "px"
                                    }, 75, function () {
                                        logWin.animate(
                                            {
                                                left: "+=" + values[0] + "px"
                                            }, 75, function () { }
                                        )
                                    }
                                )
                            }
                        )
                    }
                )
            }
        )
    }
    setPassword(e) {
        this.setState({ password: e.target.value })
    }
    setUsername(e) {
        this.setState({ username: e.target.value })
    }
    render() {
        return [
            <div key="login_background" id="loginBackgrnd"></div>,
            <div key="login_win" id="loginWindow">
                <div id="loginTitleBar">
                    <label>HSB-# Manager</label>
                </div>
                <div id="loginContent">
                    <input type="username" name="username" placeholder="Username" onChange={(e) => this.setUsername(e)} />
                    <br />
                    <input type="password" name="password" placeholder="Password" onChange={(e) => this.setPassword(e)} />
                    <br />
                    <button onClick={() => this.onLogin()}>Login</button>
                </div>
                <div id="loginBottomBar">
                    HSB-# Manager - {getCookie("managerVersion")}
                </div>
            </div>
        ];
    }
}